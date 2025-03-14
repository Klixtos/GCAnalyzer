using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace GCAnalyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AvoidGCCollectAnalyzer : DiagnosticAnalyzer
    {
        // Diagnostic ID - should be unique
        public const string DiagnosticId = "GCA001";

        // Category for the analyzer
        private const string Category = "Performance";

        // Create a diagnostic category and severity
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: Resources.AvoidUsingGCCollectTitle,
            messageFormat: Resources.AvoidUsingGCCollectMessageFormat,
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Resources.AvoidUsingGCCollectDescription,
            helpLinkUri: AnalyzerConstants.GetHelpLink(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            // Avoid analyzing generated code
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register for method invocation analysis
            context.RegisterSyntaxNodeAction(AnalyzeMethodInvocation, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeMethodInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            
            // Check if it's a member access (like GC.Collect())
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                // Check if it's accessing a member named "Collect"
                if (memberAccess.Name.Identifier.ValueText == "Collect")
                {
                    // Check if it's on the GC class
                    if (memberAccess.Expression is IdentifierNameSyntax identifierName && 
                        identifierName.Identifier.ValueText == "GC")
                    {
                        // Report diagnostic
                        var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                    else
                    {
                        // Handle case where it might be System.GC.Collect()
                        var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess);
                        if (symbolInfo.Symbol != null && symbolInfo.Symbol.ContainingType?.Name == "GC" && 
                            symbolInfo.Symbol.ContainingNamespace?.Name == "System")
                        {
                            // Report diagnostic
                            var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation());
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }
} 