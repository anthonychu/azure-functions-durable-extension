﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Microsoft.Azure.WebJobs.Extensions.DurableTask.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TimerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DF0103";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.TimerAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString V2MessageFormat = new LocalizableResourceString(nameof(Resources.DeterministicAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString V1MessageFormat = new LocalizableResourceString(nameof(Resources.V1TimerAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.DeterministicAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = SupportedCategories.Orchestrator;
        public const DiagnosticSeverity severity = DiagnosticSeverity.Warning;
        
        private static DiagnosticDescriptor V1Rule = new DiagnosticDescriptor(DiagnosticId, Title, V2MessageFormat, Category, severity, isEnabledByDefault: true, description: Description);
        private static DiagnosticDescriptor V2Rule = new DiagnosticDescriptor(DiagnosticId, Title, V2MessageFormat, Category, severity, isEnabledByDefault: true, description: Description);

        private static DurableVersion version;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(V2Rule, V1Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeIdentifierTask, SyntaxKind.IdentifierName);
            context.RegisterSyntaxNodeAction(AnalyzeIdentifierThread, SyntaxKind.IdentifierName);
        }

        private static void AnalyzeIdentifierTask(SyntaxNodeAnalysisContext context)
        {
            var identifierName = context.Node as IdentifierNameSyntax;
            if (identifierName != null)
            {
                var semanticModel = context.SemanticModel;
                version = SyntaxNodeUtils.GetDurableVersion(semanticModel);

                var identifierText = identifierName.Identifier.ValueText;
                if (identifierText == "Delay")
                {
                    var memberAccessExpression = identifierName.Parent;
                    var invocationExpression = memberAccessExpression.Parent;
                    var memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpression).Symbol;

                    if (!memberSymbol?.ToString().StartsWith("System.Threading.Tasks.Task") ?? true)
                    {
                        return;
                    }
                    else if (!SyntaxNodeUtils.IsInsideOrchestrator(identifierName) && !SyntaxNodeUtils.IsMarkedDeterministic(identifierName))
                    {
                        return;
                    }
                    else
                    {
                        if (TryGetRuleFromVersion(out DiagnosticDescriptor rule))
                        {
                            var diagnostic = Diagnostic.Create(rule, invocationExpression.GetLocation(), memberAccessExpression);

                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }

        private static void AnalyzeIdentifierThread(SyntaxNodeAnalysisContext context)
        {
            var identifierName = context.Node as IdentifierNameSyntax;
            if (identifierName != null)
            {
                var semanticModel = context.SemanticModel;
                version = SyntaxNodeUtils.GetDurableVersion(semanticModel);

                var identifierText = identifierName.Identifier.ValueText;
                if (identifierText == "Sleep")
                {
                    var memberAccessExpression = identifierName.Parent;
                    var invocationExpression = memberAccessExpression.Parent;
                    var memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpression).Symbol;

                    if (!memberSymbol?.ToString().StartsWith("System.Threading.Thread") ?? true)
                    {
                        return;
                    }
                    else if (!SyntaxNodeUtils.IsInsideOrchestrator(identifierName) && !SyntaxNodeUtils.IsMarkedDeterministic(identifierName))
                    {
                        return;
                    }
                    else
                    {
                        if (TryGetRuleFromVersion(out DiagnosticDescriptor rule))
                        {
                            var diagnostic = Diagnostic.Create(rule, invocationExpression.GetLocation(), memberAccessExpression);

                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }

        private static bool TryGetRuleFromVersion(out DiagnosticDescriptor rule)
        {
            if (version.Equals(DurableVersion.V1))
            {
                rule = V1Rule;
                return true;
            }
            else if (version.Equals(DurableVersion.V2))
            {
                rule = V2Rule;
                return true;
            }

            rule = null;
            return false;
        }
    }
}
