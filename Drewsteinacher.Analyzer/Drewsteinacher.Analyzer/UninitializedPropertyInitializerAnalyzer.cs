using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Drewsteinacher.Analyzer;

/// <summary>
/// An analyzer to prevent a runtime NullReferenceException during object creation due to missing initialization.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UninitializedPropertyInitializerAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DRWSTR0001";
    private static readonly LocalizableString Title = "Uninitialized property assigned in member initializer";
    private static readonly LocalizableString MessageFormat
        = "To prevent runtime errors, add 'new()' here or initialize property '{0}' with a non-null default value";
    private static readonly LocalizableString Description
        = "Properties without initializers cannot be used directly in a member initializer without causing a NullReferenceException at runtime. Either use 'new()' syntax in the member initializer or initialize the property with a default value (e.g.,'Property { get; set; } = new ClassName()').";
    private const string Category = "Usage";
    private const string DocumentationLinkUri
        = "https://github.com/drewsteinacher/Drewsteinacher.Analyzer/blob/main/docs/DRWSTR0001.md";

    private static readonly DiagnosticDescriptor Rule
        = new(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: DocumentationLinkUri);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeOperation, OperationKind.ObjectOrCollectionInitializer);
    }

    /// <summary>
    /// Executed on the completion of the semantic analysis associated with the Invocation operation.
    /// </summary>
    /// <param name="context">Operation context.</param>
    private static void AnalyzeOperation(OperationAnalysisContext context)
    {
        if (context.Operation is not IObjectOrCollectionInitializerOperation
            {
                Syntax: InitializerExpressionSyntax initializerSyntax,
                Parent: IMemberInitializerOperation memberInitializer
            })
        {
            return;
        }

        if (memberInitializer.InitializedMember is not IPropertyReferenceOperation propertyRef)
        {
            return;
        }

        var propertySymbol = propertyRef.Property;
        
        var declarationSyntax = propertySymbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (declarationSyntax == null)
        {
            return;
        }

        var propDeclaration = declarationSyntax.GetSyntax() as PropertyDeclarationSyntax;
        
        var hasInitializer = propDeclaration?.Initializer != null;
        if (hasInitializer)
        {
            return;
        }

        // Highlight from the start of the initializer to the opening brace
        // 1. Shows where the 'new' syntax should go
        // 2. Is a larger target than the opening brace by itself
        // 3. Is less obnoxious than highlighting the entire initializer
        // 4. Allows for showing nested problems individually
        var location = Location.Create(
            memberInitializer.Syntax.SyntaxTree,
            TextSpan.FromBounds(memberInitializer.Syntax.SpanStart, initializerSyntax.OpenBraceToken.Span.End));

        var diagnostic = Diagnostic.Create(Rule, location, propertySymbol.Name);

        context.ReportDiagnostic(diagnostic);
    }
}