namespace Sitecore.Support.ExperienceExplorer.Business.Pipelines.HttpRequestProcessed
{
  #region "dependencies"

  using Sitecore.Diagnostics;
  using Sitecore.ExperienceExplorer.Business.Entities.Rules;
  using Sitecore.ExperienceExplorer.Business.Factories;
  using Sitecore.ExperienceExplorer.Business.Helpers;
  using Sitecore.ExperienceExplorer.Business.Managers;
  using Sitecore.ExperienceExplorer.Business.Strategies.Rules;
  using Sitecore.Pipelines.HttpRequest;

  #endregion
  public class RetrieveRenderingsPipeline : Sitecore.ExperienceExplorer.Business.Pipelines.HttpRequestProcessed.RetrieveRenderingsPipeline
  {
    public RetrieveRenderingsPipeline():base() {}
    public override void Process(HttpRequestArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (!PageModeHelper.IsExperienceMode || PageModeHelper.IsInternalRequest)
      {
        return;
      }

      var item = Context.Item;
      if (item == null)
      {
        return;
      }

      var model = ModuleManager.Model;
      if (model == null)
      {
        return;
      }

      var renderings = AbstractFactory.Create<RenderingsDtos, RuleStrategy>(new RuleStrategy(item));
      model.Renderings = renderings;
      ModuleManager.Model = model;
    }
  }
}