using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Optimization;

namespace Raven.Client.AspNetProviders.DemoApp
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                        "~/Scripts/bootstrap*"));

            bundles.Add(new StyleBundle("~/Content/Style").Include(
                       "~/Content/Style.css"));

            bundles.Add(new StyleBundle("~/Content/bootstrap").Include(
                       "~/Content/bootstrap.css",
                       "~/Content/bootstrap-responsive.css"));
        }
    }
}