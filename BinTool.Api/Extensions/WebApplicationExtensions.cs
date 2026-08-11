namespace BinTool.Api.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Builds the request pipeline. Order matters here in a way the registration side does
    /// not: authentication has to establish who the caller is before authorization can rule
    /// on what they may do.
    /// </summary>
    public static WebApplication UseApplicationPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "BinTool API v1");
            });
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
