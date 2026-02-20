using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using TMSBilling.Services;

namespace TMSBilling.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;

        public EmailTemplateService(
            IRazorViewEngine razorViewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider)
        {
            _razorViewEngine = razorViewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }

        public async Task<string> RenderTemplateAsync<TModel>(string viewName, TModel model)
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = _serviceProvider
            };

            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor()
            );

            using var sw = new StringWriter();

            //var viewResult = _razorViewEngine.FindView(actionContext, viewName, false);

            //if (!viewResult.Success)
            //    throw new InvalidOperationException($"Template '{viewName}' tidak ditemukan.");

            // SESUDAH - ganti dengan ini
            var viewResult = _razorViewEngine.GetView(
                executingFilePath: null,
                viewPath: $"~/Views/EmailTemplates/{viewName}.cshtml",
                isMainPage: false
            );

            if (!viewResult.Success)
                throw new InvalidOperationException($"Template '{viewName}' tidak ditemukan di Views/EmailTemplates/.");

            var viewDictionary = new ViewDataDictionary<TModel>(
                new EmptyModelMetadataProvider(),
                new ModelStateDictionary())
            {
                Model = model
            };

            var tempData = new TempDataDictionary(
                actionContext.HttpContext,
                _tempDataProvider
            );

            var viewContext = new ViewContext(
                actionContext,
                viewResult.View,
                viewDictionary,
                tempData,
                sw,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);

            return sw.ToString();
        }
    }
}