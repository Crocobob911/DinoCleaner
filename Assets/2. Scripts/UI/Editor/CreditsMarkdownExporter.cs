using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DinoCleaner.UI.Editor
{
    /// <summary>
    /// 화면과 같은 credits.json·표기 규칙으로 저장소 루트에 CREDITS.md를 만듭니다.
    /// 메뉴: DinoCleaner/UI/Export CREDITS.md
    /// </summary>
    public static class CreditsMarkdownExporter
    {
        private const string JsonPath = "Assets/5. UI/Credits/credits.json";
        private const string OutputFileName = "CREDITS.md";
        private const string DefaultTitle = "Credits";

        [MenuItem("DinoCleaner/UI/Export CREDITS.md")]
        public static void Export()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(JsonPath);
            if (json == null)
            {
                Debug.LogError($"[Credits] 크레딧 데이터를 찾지 못했습니다: {JsonPath}");
                return;
            }

            CreditsData data = CreditsFormatter.Parse(json.text, JsonPath);
            if (data == null) return;

            string markdown = BuildMarkdown(data, CreditsFormatter.BuildSections(data, JsonPath));
            string outputPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, OutputFileName);
            File.WriteAllText(outputPath, markdown, new UTF8Encoding(false));
            Debug.Log($"[Credits] {OutputFileName} 생성 완료: {outputPath}");
        }

        private static string BuildMarkdown(CreditsData data, List<CreditsSectionView> sections)
        {
            var sb = new StringBuilder();
            string title = string.IsNullOrWhiteSpace(data.gameTitle) ? DefaultTitle : $"{data.gameTitle.Trim()} Credits";
            sb.Append("# ").Append(title).Append('\n');
            sb.Append('\n');
            sb.Append("<!-- Generated from ").Append(JsonPath)
              .Append(" via DinoCleaner/UI/Export CREDITS.md. Edit the JSON, not this file. -->\n");

            foreach (CreditsSectionView section in sections)
            {
                sb.Append('\n');
                sb.Append("## ").Append(section.Title).Append('\n');
                sb.Append('\n');
                foreach (CreditsLine line in section.Lines)
                {
                    AppendLine(sb, line);
                }
            }
            return sb.ToString();
        }

        // 새 줄 스타일이 생기면 여기에 case 하나만 추가합니다.
        private static void AppendLine(StringBuilder sb, CreditsLine line)
        {
            sb.Append("- ");
            switch (line.Style)
            {
                case CreditsLineStyle.Person:
                    sb.Append("**").Append(line.Text).Append("**");
                    if (!string.IsNullOrEmpty(line.Detail)) sb.Append(" — ").Append(line.Detail);
                    break;
                default:
                    sb.Append(line.Text);
                    break;
            }

            if (!string.IsNullOrEmpty(line.MarkdownNote)) sb.Append(" _(").Append(line.MarkdownNote).Append(")_");
            sb.Append('\n');
        }
    }
}
