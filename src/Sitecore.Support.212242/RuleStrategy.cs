﻿namespace Sitecore.Support.ExperienceExplorer.Business.Strategies.Rules
{
  using System.Collections.Generic;
  using System.Linq;
  using System.Xml.Linq;

  using Sitecore.Data;
  using Sitecore.Data.Fields;
  using Sitecore.Data.Items;
  using Sitecore.ExperienceExplorer.Business.Constants;
  using Sitecore.ExperienceExplorer.Business.Entities;
  using Sitecore.ExperienceExplorer.Business.Entities.Renderings;
  using Sitecore.ExperienceExplorer.Business.Entities.Rules;
  using Sitecore.ExperienceExplorer.Business.Helpers;
  using Sitecore.ExperienceExplorer.Business.Managers;
  using Sitecore.ExperienceExplorer.Business.Utilities;
  using Sitecore.ExperienceExplorer.Business.Utilities.Extensions;
  using Sitecore.Globalization;
  using Sitecore.Layouts;
  using Sitecore.Rules;
  using Sitecore.Rules.ConditionalRenderings;
  using Sitecore.Web.UI;
  public class RuleStrategy : Sitecore.ExperienceExplorer.Business.Strategies.Rules.RuleStrategy
  {
    public RuleStrategy(Item item) : base(item) { }
    public RuleStrategy(PresetDefinition presetDefinition) : base(presetDefinition) { }
    public override RenderingDto GetRenderingDto(RenderingReference renderingReference)
    {
      RenderingDto renderingDto = new RenderingDto();
      Item item = Context.Database.GetItem(renderingReference.RenderingID);
      if (item != null)
      {
        renderingDto.RenderingId = item.ID.ToString();
        renderingDto.Name = item.Name;
        renderingDto.Icon = IconHelpers.GetIconPath(item, ImageDimension.id16x16);
      }
      renderingDto.Conditions = renderingReference.Settings.Conditions;
      renderingDto.DataSource = renderingReference.Settings.DataSource;
      renderingDto.MultiVariantTest = renderingReference.Settings.MultiVariateTest;
      renderingDto.Placeholder = renderingReference.Settings.Placeholder;
      renderingDto.Parameters = renderingReference.Settings.Parameters;
      renderingDto.RuleDtos = new List<RuleDto>();
      if (renderingReference.Settings.Rules != null && renderingReference.Settings.Rules.Rules != null)
      {
        foreach (string item2 in from x in renderingReference.Settings.Conditions.Split('|')
                                 where !string.IsNullOrEmpty(x)
                                 select x)
        {
          List<RuleDto> conditionRuleDto = this.GetConditionRuleDto(renderingReference, item2);
          if (conditionRuleDto != null)
          {
            renderingDto.RuleDtos.AddRange(conditionRuleDto);
          }
        }
        {
          foreach (Rule<ConditionalRenderingsRuleContext> rule in renderingReference.Settings.Rules.Rules)
          {
            RuleDto ruleDto = this.GetRuleDto(renderingReference, rule);
            if (ruleDto != null)
            {
              renderingDto.RuleDtos.Add(ruleDto);
            }
          }
          return renderingDto;
        }
      }
      return renderingDto;
    }
    private List<RuleDto> GetConditionRuleDto(RenderingReference renderingReference, string condition)
    {
      List<RuleDto> list = new List<RuleDto>();
      Item item = Database.GetDatabase("master").GetItem(new ID(condition));
      string fieldAsString = item.GetFieldAsString(IDs.ConditionalRenderingRule);
      XDocument xDocument = XDocument.Parse(fieldAsString);
      ConditionalRenderingsRuleContext conditionalRenderingsRuleContext = new ConditionalRenderingsRuleContext(this.RenderingReferences, renderingReference);
      conditionalRenderingsRuleContext.Item = this.CurrentItem;
      ConditionalRenderingsRuleContext ruleContext = conditionalRenderingsRuleContext;
      IEnumerable<XElement> enumerable = from r in xDocument.Descendants("rule")
                                         select (r);
      foreach (XElement item2 in enumerable)
      {
        RuleDto ruleDto = new RuleDto();
        ruleDto.UniqueId = condition;
        string text = item.GetFieldAsString(IDs.ConditionalRenderingName);
        if (item2.Attribute("name") != null)
        {
          text = item2.Attribute("name").Value;
        }
        XElement xElement = item2;
        XElement xElement2 = xElement.Descendants("conditions").FirstOrDefault();
        if (xElement2 != null)
        {
          XElement parent = xElement2.Elements().FirstOrDefault();
          ConditionsHelper conditionsHelper = new ConditionsHelper();
          List<Condition> list2 = new List<Condition>();
          conditionsHelper.GetConditionsRecursively(list2, parent, false, "");
          ruleDto.Conditions = new List<ConditionDto>();
          foreach (Condition item3 in list2)
          {
            string rulesXml = "<rules><rule><conditions>" + item3.Node + "</conditions></rule></rules>";
            RuleList<RuleContext> ruleList = RuleFactory.ParseRules<RuleContext>(Database.GetDatabase("web"), rulesXml);
            item3.IsTrue = ruleList.Rules.FirstOrDefault().Evaluate(ruleContext);
            ruleDto.Conditions.Add(new ConditionDto
            {
              Text = item3.Text,
              IsTrue = item3.IsTrue,
              Indent = item3.Indent
            });
          }
        }
        if (text != null)
        {
          ruleDto.Name = text;
        }
        string rulesXml2 = "<rules>" + item2.ToString() + "</rules>";
        RuleList<RuleContext> ruleList2 = RuleFactory.ParseRules<RuleContext>(Database.GetDatabase("web"), rulesXml2);
        Rule<RuleContext> rule = ruleList2.Rules.FirstOrDefault();
        if (rule != null)
        {
          ruleDto.Selected = rule.Evaluate(ruleContext);
        }
        list.Add(ruleDto);
      }
      return list;
    }
  }
}