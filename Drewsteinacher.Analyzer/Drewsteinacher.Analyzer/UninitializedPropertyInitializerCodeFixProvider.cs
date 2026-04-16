using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Drewsteinacher.Analyzer;

/// <summary>
/// A code fix provider to automatically add 'new' object creation syntax to the affected initializer expression.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UninitializedPropertyInitializerCodeFixProvider)), Shared]
public class UninitializedPropertyInitializerCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(UninitializedPropertyInitializerAnalyzer.DiagnosticId);

    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.Single();
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnosticNode = root?.FindNode(diagnosticSpan);
        
        if (diagnosticNode is not InitializerExpressionSyntax initializerExpressionSyntax)
        {
            return;
        }

        // Get the semantic model to determine the actual property type
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        var operation = semanticModel?.GetOperation(initializerExpressionSyntax, context.CancellationToken);

        var objectCreationPreview = operation switch
        {
            // TODO: Show 'new Type {...}' eventually?
            // { Type.Name: not (null or "") } => $"new {operation.Type.Name} {{...}}",
            
            // Fall back to 'new() {...}'
            _ => "new"
        };

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Add '{objectCreationPreview}' to prevent runtime error",
                createChangedSolution: c => AddNewObjectSyntaxAsync(context.Document, initializerExpressionSyntax, c),
                equivalenceKey: nameof(UninitializedPropertyInitializerCodeFixProvider)),
            diagnostic);
    }

    /// <summary>
    /// Executed on the quick fix action raised by the user.
    /// </summary>
    /// <param name="document">Affected source file.</param>
    /// <param name="initializerExpressionSyntax">Highlighted Initializer Expression Syntax Node.</param>
    /// <param name="cancellationToken">Any fix is cancellable by the user, so we should support the cancellation token.</param>
    /// <returns>Clone of the solution with updates: Object creation expression around the Initializer Expression.</returns>
    private async Task<Solution> AddNewObjectSyntaxAsync(Document document,
        InitializerExpressionSyntax initializerExpressionSyntax, CancellationToken cancellationToken)
    {
        // Determine object creation type name
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var operation = semanticModel?.GetOperation(initializerExpressionSyntax, cancellationToken);
        
        // TODO: Make this configurable via .editorconfig?
        var (type, arguments) = operation switch
        {
            // Prefer 'new Type {...}'
            { Type.Name: not (null or "") } => (operation.Type.Name, null), // new Type {...}
            
            // Fall back to 'new() {...}'
            _ => (string.Empty, SyntaxFactory.ArgumentList()), // new() {...}
        };
        
        // Create a new object creation expression for the initializer
        var newObjectCreation = SyntaxFactory.ObjectCreationExpression(
            SyntaxFactory.IdentifierName(type),
            arguments,
            initializerExpressionSyntax);
        
        // Maintain surrounding indentation
        // TODO: Remove trivia between the 'new' stuff and the initializer expression to improve formatting
        var newObjectWithTrivia = newObjectCreation.WithLeadingTrivia(initializerExpressionSyntax.GetLeadingTrivia());
        
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document.Project.Solution;
        }

        var newRoot = root.ReplaceNode(initializerExpressionSyntax, newObjectWithTrivia);
        var newDocument = document.WithSyntaxRoot(newRoot);
        
        return newDocument.Project.Solution;
    }
}