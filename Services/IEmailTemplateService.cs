namespace TMSBilling.Services
{
    public interface IEmailTemplateService
    {
        Task<string> RenderTemplateAsync<TModel>(string viewName, TModel model);
    }
}