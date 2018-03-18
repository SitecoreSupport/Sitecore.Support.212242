// Sitecore.ExperienceExplorer.Business.Helpers.ConditionsHelper
namespace Sitecore.Support.ExperienceExplorer.Business.Helpers
{
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.Data.Managers;
  using Sitecore.Diagnostics;
  using Sitecore.ExperienceExplorer.Business.Entities.Renderings;
  using Sitecore.Extensions.StringExtensions;
  using Sitecore.Extensions.XElementExtensions;
  using Sitecore.Globalization;
  using Sitecore.SecurityModel;
  using Sitecore.Web;
  using System;
  using System.Collections.Generic;
  using System.Text;
  using System.Text.RegularExpressions;
  using System.Xml.Linq;

  /// <summary>
  /// The conditions helper class, for basic logic were used RulesRenderer class that is able to parse rules XML.
  /// </summary>
  public class ConditionsHelper
  {

    private string webDatabaseName = Sitecore.Configuration.Settings.GetSetting("ExperienceExplorer.WebDatabase", "web");

    public ConditionsHelper() : base() { }    

    public void GetConditionsRecursively(List<Condition> output, XElement parent, bool indent = false, string addtionalOperator = "")
    {
      Assert.ArgumentNotNull(parent, "parent");
      if (parent.Name.LocalName == "condition")
      {
        this.GetCondition(output, parent, indent, addtionalOperator);
      }
      else if (parent.Name.LocalName == "not")
      {
        XElement xElement = parent.Element(0);
        if (xElement != null)
        {
          if (!string.IsNullOrEmpty(addtionalOperator))
          {
            addtionalOperator += " ";
          }
          addtionalOperator += Translate.Text(parent.Name.LocalName);
          this.GetConditionsRecursively(output, xElement, indent, addtionalOperator);
        }
      }
      else
      {
        XElement xElement2 = parent.Element(0);
        if (xElement2 != null)
        {
          XElement xElement3 = parent.Element(1);
          if (xElement3 != null)
          {
            this.GetConditionsRecursively(output, xElement2, indent, addtionalOperator);
            bool indent2 = indent;
            addtionalOperator = this.GetBinaryOperator(parent, ref indent2);
            this.GetConditionsRecursively(output, xElement3, indent2, addtionalOperator);
          }
        }
      }
    }

    private string GetBinaryOperator(XElement operatorElement, ref bool indent)
    {
      Assert.ArgumentNotNull(operatorElement, "operatorElement");
      string localName = operatorElement.Name.LocalName;
      XElement xElement = operatorElement.Element(1);
      if (xElement == null)
      {
        return string.Empty;
      }
      if (localName == "and")
      {
        indent = true;
      }
      return "<strong>" + Translate.Text(localName) + "</strong>";
    }

    private void GetCondition(List<Condition> output, XElement condition, bool intend, string additionalOperator)
    {
      Assert.ArgumentNotNull(output, "output");
      Assert.ArgumentNotNull(condition, "condition");
      string attributeValue = condition.GetAttributeValue("id");
      if (!string.IsNullOrEmpty(attributeValue))
      {
        string attributeValue2 = condition.GetAttributeValue("uid");
        if (!string.IsNullOrEmpty(attributeValue2))
        {
          Item item = default(Item);
          using (new SecurityDisabler())
          {
            item = Database.GetDatabase(webDatabaseName).GetItem(attributeValue);
          }
          if (item == null)
          {
            output.Add(new Condition
            {
              Text = Translate.Text("Unknown condition: {0}", attributeValue)
            });
          }
          else
          {
            string itemText = this.GetItemText(item);
            string conditionPrefix = this.GetConditionPrefix(condition, ref itemText);
            string conditionText = this.GetConditionText(condition, itemText);
            Condition condition2 = new Condition();
            condition2.Text = additionalOperator + " " + conditionPrefix + conditionText;
            condition2.Node = condition.ToString();
            condition2.Indent = intend;
            Condition item2 = condition2;
            output.Add(item2);
          }
        }
      }
    }

    private string GetItemText(Item item)
    {
      string text = ((BaseItem)item)["Text"];
      if (!string.IsNullOrEmpty(text))
      {
        return text;
      }
      Language language = LanguageManager.GetLanguage("en", item.Database);
      if (language != (Language)null)
      {
        Item item2 = item.Database.GetItem(item.ID, language);
        if (item2 != null)
        {
          text = ((BaseItem)item2)["Text"];
          if (!string.IsNullOrEmpty(text))
          {
            return text;
          }
        }
      }
      return string.Empty;
    }

    private string GetConditionPrefix(XElement condition, ref string text)
    {
      string str = string.Empty;
      Assert.ArgumentNotNull(condition, "condition");
      Assert.ArgumentNotNull(text, "text");
      int num = text.IndexOf(' ');
      if (num < 0)
      {
        return text;
      }
      text = text.Mid(num + 1);
      if (condition.GetAttributeValue("except") == "true")
      {
        str = Translate.Text("except") + " ";
      }
      return str + Translate.Text("where") + " ";
    }

    private string GetConditionText(XElement action, string text)
    {
      Assert.ArgumentNotNull(action, "action");
      Assert.ArgumentNotNull(text, "text");
      string attributeValue = action.GetAttributeValue("uid");
      Assert.IsNotNullOrEmpty(attributeValue, "uid is null or empty");
      text = WebUtil.HtmlEncode(text);
      text = Regex.Replace(text, "\\[([^\\]])*\\]", delegate (Match match)
      {
        Assert.ArgumentNotNull(match, "match");
        string value = match.Value;
        value = value.Mid(1, value.Length - 2);
        string[] array = value.Split(',');
        string text2 = array[0];
        string text3 = action.GetAttributeValue(text2);
        if (string.IsNullOrEmpty(text3))
        {
          text3 = text2;
          if (array.Length >= 4)
          {
            text3 = array[3];
          }
        }
        else if (ID.IsID(text3))
        {
          Item item = Database.GetDatabase(webDatabaseName).GetItem(text3);
          if (item != null)
          {
            text3 = item.DisplayName;
          }
        }
        else if (text3.Contains("|"))
        {
          StringBuilder stringBuilder = new StringBuilder();
          StringBuilder stringBuilder2 = new StringBuilder();
          bool flag = false;
          int num = 0;
          string[] array2 = text3.Split(new char[1]
            {
                    '|'
              }, StringSplitOptions.RemoveEmptyEntries);
          foreach (string text4 in array2)
          {
            if (!ID.IsID(text4))
            {
              flag = true;
              break;
            }
            Item item2 = Database.GetDatabase(webDatabaseName).GetItem(text4);
            if (item2 != null)
            {
              if (num > 0)
              {
                stringBuilder.Append(", ");
                stringBuilder2.Append(", ");
              }
              num++;
              stringBuilder.Append(item2.DisplayName);
              stringBuilder2.Append(item2.Paths.Path);
            }
          }
          if (!flag)
          {
            text3 = stringBuilder.ToString();
          }
        }
        return text3;
      });
      return text;
    }
  }
}