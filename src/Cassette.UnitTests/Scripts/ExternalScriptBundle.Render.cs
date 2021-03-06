﻿using System;
using Cassette.BundleProcessing;
using Moq;
using Should;
using Xunit;

namespace Cassette.Scripts
{
    public class ExternalScriptBundleRender_Tests
    {
        public ExternalScriptBundleRender_Tests()
        {
            settings = new CassetteSettings()
            {
                IsDebuggingEnabled = false
            };
            fallbackRenderer = new Mock<IBundleHtmlRenderer<ScriptBundle>>();
        }

        readonly CassetteSettings settings;
        readonly Mock<IBundleHtmlRenderer<ScriptBundle>> fallbackRenderer;

        [Fact]
        public void WhenRenderExternalScriptBundle_ThenHtmlIsScriptElement()
        {
            var bundle = new ExternalScriptBundle("http://test.com/") { Pipeline = Mock.Of<IBundlePipeline<ScriptBundle>>() };
            var html = Render(bundle);
            html.ShouldEqual("<script src=\"http://test.com/\" type=\"text/javascript\"></script>");
        }

        [Fact]
        public void WhenRenderExternalScriptBundleWithCondition_ThenHtmlIsScriptElementWithConditional()
        {
            var bundle = new ExternalScriptBundle("http://test.com/") { Condition = "CONDITION", Pipeline = Mock.Of<IBundlePipeline<ScriptBundle>>() };
            var html = Render(bundle);

            html.ShouldEqual(
                "<!--[if CONDITION]>" + Environment.NewLine +
                "<script src=\"http://test.com/\" type=\"text/javascript\"></script>" + Environment.NewLine +
                "<![endif]-->");
        }

        [Fact]
        public void WhenRenderExternalScriptBundleWithNotIECondition_ThenHtmlIsScriptElementWithConditionalButLeavesScriptVisibleToAllBrowsers()
        {
            var bundle = new ExternalScriptBundle("http://test.com/") { Condition = "(gt IE 9)|!(IE)", Pipeline = Mock.Of<IBundlePipeline<ScriptBundle>>() };

            var html = bundle.Render(bundle);

            html.ShouldEqual(
                "<!--[if " + bundle.Condition + "]><!-->" + Environment.NewLine +
                "<script src=\"http://test.com/\" type=\"text/javascript\"></script>" + Environment.NewLine +
                "<!-- <![endif]-->");
        }

        [Fact]
        public void WhenRenderExternalScriptBundleWithHtmlAttributes_ThenHtmlIsScriptElementWithExtraAttributes()
        {
            var bundle = new ExternalScriptBundle("http://test.com/") { Pipeline = Mock.Of<IBundlePipeline<ScriptBundle>>() };
            bundle.HtmlAttributes["class"] = "foo";

            var html = Render(bundle);

            html.ShouldEqual("<script src=\"http://test.com/\" type=\"text/javascript\" class=\"foo\"></script>");
        }

        [Fact]
        public void WhenRenderExternalScriptBundleWithLocalAssetsAndIsDebugMode_ThenFallbackRendererUsed()
        {
            var bundle = new ExternalScriptBundle("http://test.com/", "test") { Pipeline = Mock.Of<IBundlePipeline<ScriptBundle>>() };
            bundle.Assets.Add(new StubAsset());
            fallbackRenderer.Setup(r => r.Render(bundle))
                            .Returns("FALLBACK");
            settings.IsDebuggingEnabled = true;

            var html = Render(bundle);

            html.ShouldEqual("FALLBACK");
        }

        [Fact]
        public void WhenRenderExternalScriptBundleWithFallbackAsset_ThenHtmlContainsFallbackScript()
        {
            var bundle = new ExternalScriptBundle("http://test.com/", "test", "CONDITION") { Pipeline = Mock.Of<IBundlePipeline<ScriptBundle>>() };
            bundle.Assets.Add(new StubAsset());

            fallbackRenderer.Setup(r => r.Render(bundle))
                            .Returns("FALLBACK");

            var html = Render(bundle);

            html.ShouldEqual(
                "<script src=\"http://test.com/\" type=\"text/javascript\"></script>" + Environment.NewLine +
                "<script type=\"text/javascript\">" + Environment.NewLine +
                "if(CONDITION){" + Environment.NewLine +
                "document.write('FALLBACK');" + Environment.NewLine +
                "}" + Environment.NewLine +
                "</script>"
            );
        }

        [Fact]
        public void WhenRenderExternalScriptBundleWithFallbackAsset_ThenHtmlEscapesFallbackScriptTags()
        {
            var bundle = new ExternalScriptBundle("http://test.com/", "test", "CONDITION") { Pipeline = Mock.Of<IBundlePipeline<ScriptBundle>>() };
            bundle.Assets.Add(new StubAsset());

            fallbackRenderer.Setup(r => r.Render(bundle))
                            .Returns("<script></script>");

            var html = Render(bundle);

            html.ShouldContain(@"<script><\/script>");
        }

        [Fact]
        public void GivenExternalScriptBundleWithFallbackAssetsAndDebugMode_WhenRender_ThenOnlyOutputFallbackScripts()
        {
            settings.IsDebuggingEnabled = true;

            var bundle = new ExternalScriptBundle("http://test.com/", "test", "CONDITION") { Pipeline = Mock.Of<IBundlePipeline<ScriptBundle>>() };
            bundle.Assets.Add(new StubAsset());

            fallbackRenderer.Setup(r => r.Render(bundle))
                            .Returns("<script></script>");

            var html = Render(bundle);

            html.ShouldEqual("<script></script>");
        }

        [Fact]
        public void WhenRenderExternalScriptBundleWithNoLocalAssetsAndIsDebugMode_ThenNormalScriptElementIsReturned()
        {
            var bundle = new ExternalScriptBundle("http://test.com/", "test") { Pipeline = Mock.Of<IBundlePipeline<ScriptBundle>>() };
            settings.IsDebuggingEnabled = true;

            var html = Render(bundle);

            html.ShouldEqual("<script src=\"http://test.com/\" type=\"text/javascript\"></script>");
        }

        string Render(ExternalScriptBundle bundle)
        {
            bundle.Renderer = fallbackRenderer.Object;
            bundle.Process(settings);
            return bundle.Render();
        }
    }
}