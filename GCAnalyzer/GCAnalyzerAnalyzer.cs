using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using GCAnalyzer.Analyzers;

namespace GCAnalyzer
{
    /// <summary>
    /// Main analyzer entry point that registers all the individual analyzers
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class GCAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        // References to individual analyzers
        private readonly AvoidGCCollectAnalyzer _avoidGCCollectAnalyzer = new AvoidGCCollectAnalyzer();
        private readonly UseGCKeepAliveAnalyzer _useGCKeepAliveAnalyzer = new UseGCKeepAliveAnalyzer();
        private readonly ResourceDisposalAnalyzer _resourceDisposalAnalyzer = new ResourceDisposalAnalyzer();
        private readonly InterfaceMethodExceptionAnalyzer _interfaceMethodExceptionAnalyzer = new InterfaceMethodExceptionAnalyzer();
        private readonly FieldNamingConventionAnalyzer _fieldNamingConventionAnalyzer = new FieldNamingConventionAnalyzer();

        /// <summary>
        /// Returns all diagnostics that this analyzer supports
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                _avoidGCCollectAnalyzer.SupportedDiagnostics[0],
                _useGCKeepAliveAnalyzer.SupportedDiagnostics[0],
                _resourceDisposalAnalyzer.SupportedDiagnostics[0],
                _interfaceMethodExceptionAnalyzer.SupportedDiagnostics[0],
                _fieldNamingConventionAnalyzer.SupportedDiagnostics[0]
            );

        /// <summary>
        /// Initializes the analyzer by delegating to individual specialized analyzers
        /// </summary>
        public override void Initialize(AnalysisContext context)
        {
            // Configure analyzer
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Forward initialization to individual analyzers
            _avoidGCCollectAnalyzer.Initialize(context);
            _useGCKeepAliveAnalyzer.Initialize(context);
            _resourceDisposalAnalyzer.Initialize(context);
            _interfaceMethodExceptionAnalyzer.Initialize(context);
            _fieldNamingConventionAnalyzer.Initialize(context);
        }
    }
} 