using System;
using System.Collections.Generic;
using UnityEngine;

namespace DinoCleaner.UI
{
    // JsonUtility는 필드 이름으로 매핑하므로, 아래 데이터 클래스의 public 필드 이름은 credits.json 키와 같아야 합니다.

    /// <summary>
    /// credits.json 최상위 데이터입니다.
    /// </summary>
    [Serializable]
    public class CreditsData
    {
        public string gameTitle;
        public CreditsLicense[] licenses;
        public CreditsSection[] sections;
    }

    /// <summary>
    /// 라이선스 종류 하나. 에셋 항목은 licenseId로 이 값을 찾습니다.
    /// </summary>
    [Serializable]
    public class CreditsLicense
    {
        public string id;
        public string displayName;
        public string url;
        public bool attributionRequired;
        public bool shareTextFileRequired;
    }

    /// <summary>
    /// 크레딧 섹션 하나. type에 따라 entries 또는 lines를 씁니다.
    /// </summary>
    [Serializable]
    public class CreditsSection
    {
        public string id;
        public string title;
        public string type;
        public CreditsEntry[] entries;
        public string[] lines;
    }

    /// <summary>
    /// people 섹션(name, roles)과 assets 섹션(title 이하)이 함께 쓰는 항목입니다.
    /// </summary>
    [Serializable]
    public class CreditsEntry
    {
        public string name;
        public string[] roles;

        public string title;
        public string author;
        public string sourceUrl;
        public string licenseId;
        public string modification;
        public string usedFor;
    }

    public enum CreditsLineStyle
    {
        /// <summary>Text = 이름, Detail = 역할</summary>
        Person,
        /// <summary>Text = 한 줄 문장</summary>
        Text,
        /// <summary>Text = 에셋 표기 문장, MarkdownNote = CREDITS.md에만 붙는 문구</summary>
        Asset,
    }

    /// <summary>
    /// 화면과 CREDITS.md가 공통으로 쓰는, 표기 규칙이 적용된 한 줄입니다.
    /// </summary>
    public readonly struct CreditsLine
    {
        public readonly CreditsLineStyle Style;
        public readonly string Text;
        public readonly string Detail;
        public readonly string MarkdownNote;

        public CreditsLine(CreditsLineStyle style, string text, string detail = null, string markdownNote = null)
        {
            Style = style;
            Text = text;
            Detail = detail;
            MarkdownNote = markdownNote;
        }
    }

    /// <summary>
    /// 표시할 내용이 있는 섹션 하나의 결과입니다.
    /// </summary>
    public sealed class CreditsSectionView
    {
        public string Id;
        public string Title;
        public readonly List<CreditsLine> Lines = new List<CreditsLine>();
    }

    /// <summary>
    /// 섹션 type 하나를 표시용 줄로 바꿉니다. 새 type은 이 인터페이스를 구현해 CreditsFormatter에 등록하면 됩니다.
    /// </summary>
    public interface ICreditsSectionFormatter
    {
        void Format(CreditsSection section, CreditsFormatContext context, List<CreditsLine> output);
    }

    /// <summary>
    /// 포매터가 함께 쓰는 라이선스 조회와 경고 출력입니다.
    /// </summary>
    public sealed class CreditsFormatContext
    {
        private readonly Dictionary<string, CreditsLicense> _licenses = new Dictionary<string, CreditsLicense>();
        private readonly string _sourceName;

        public CreditsFormatContext(CreditsData data, string sourceName)
        {
            _sourceName = sourceName;
            if (data.licenses == null) return;

            foreach (CreditsLicense license in data.licenses)
            {
                if (license == null || string.IsNullOrWhiteSpace(license.id))
                {
                    Warn("id가 비어 있는 라이선스가 있어 건너뜁니다.");
                    continue;
                }
                if (_licenses.ContainsKey(license.id))
                {
                    Warn($"라이선스 id '{license.id}'가 중복되어 처음 것을 씁니다.");
                    continue;
                }
                _licenses.Add(license.id, license);
            }
        }

        /// <summary>
        /// licenseId로 라이선스를 찾습니다. 없으면 false를 반환합니다.
        /// </summary>
        public bool TryGetLicense(string licenseId, out CreditsLicense license)
        {
            license = null;
            return !string.IsNullOrWhiteSpace(licenseId) && _licenses.TryGetValue(licenseId, out license);
        }

        /// <summary>
        /// 데이터 문제를 크래시 없이 경고로 알립니다.
        /// </summary>
        public void Warn(string message)
        {
            Debug.LogWarning($"[Credits] {_sourceName}: {message}");
        }

        /// <summary>
        /// 값이 비어 있으면 경고하고 대체 문구를 반환합니다.
        /// </summary>
        public string Require(string value, string fieldName, string fallback, string where)
        {
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
            Warn($"{where}의 {fieldName} 값이 비어 있습니다.");
            return fallback;
        }
    }

    /// <summary>
    /// credits.json을 읽고 섹션 type별 포매터로 표시용 줄을 만듭니다.
    /// </summary>
    public static class CreditsFormatter
    {
        public const string LicenseFileNote = "License file included in project";

        private static readonly Dictionary<string, ICreditsSectionFormatter> Formatters =
            new Dictionary<string, ICreditsSectionFormatter>(StringComparer.OrdinalIgnoreCase)
            {
                { "people", new PeopleSectionFormatter() },
                { "text", new TextSectionFormatter() },
                { "assets", new AssetsSectionFormatter() },
            };

        /// <summary>
        /// JSON 문자열을 CreditsData로 읽습니다. 형식이 잘못되면 오류를 기록하고 null을 반환합니다.
        /// </summary>
        public static CreditsData Parse(string json, string sourceName)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning($"[Credits] {sourceName}: 내용이 비어 있습니다.");
                return null;
            }

            try
            {
                return JsonUtility.FromJson<CreditsData>(json);
            }
            catch (ArgumentException e)
            {
                Debug.LogError($"[Credits] {sourceName}: JSON 형식이 잘못되었습니다. {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// JSON 배열 순서대로 섹션을 만듭니다. 표시할 줄이 없는 섹션은 결과에서 빠집니다.
        /// </summary>
        public static List<CreditsSectionView> BuildSections(CreditsData data, string sourceName)
        {
            var result = new List<CreditsSectionView>();
            if (data?.sections == null) return result;

            var context = new CreditsFormatContext(data, sourceName);

            for (int i = 0; i < data.sections.Length; i++)
            {
                CreditsSection section = data.sections[i];
                if (section == null) continue;

                string label = string.IsNullOrWhiteSpace(section.id) ? $"sections[{i}]" : section.id;
                if (!Formatters.TryGetValue(section.type ?? string.Empty, out ICreditsSectionFormatter formatter))
                {
                    context.Warn($"섹션 '{label}'의 type '{section.type}'을 처리할 포매터가 없어 건너뜁니다.");
                    continue;
                }

                var view = new CreditsSectionView { Id = label };
                formatter.Format(section, context, view.Lines);
                if (view.Lines.Count == 0) continue;

                view.Title = context.Require(section.title, "title", label, $"섹션 '{label}'");
                result.Add(view);
            }
            return result;
        }
    }

    internal sealed class PeopleSectionFormatter : ICreditsSectionFormatter
    {
        public void Format(CreditsSection section, CreditsFormatContext context, List<CreditsLine> output)
        {
            if (section.entries == null) return;

            for (int i = 0; i < section.entries.Length; i++)
            {
                CreditsEntry entry = section.entries[i];
                if (entry == null) continue;

                string name = context.Require(entry.name, "name", "Unknown", $"섹션 '{section.id}' entries[{i}]");
                output.Add(new CreditsLine(CreditsLineStyle.Person, name, JoinRoles(entry.roles)));
            }
        }

        private static string JoinRoles(string[] roles)
        {
            if (roles == null) return string.Empty;

            var filled = new List<string>(roles.Length);
            foreach (string role in roles)
            {
                if (!string.IsNullOrWhiteSpace(role)) filled.Add(role.Trim());
            }
            return string.Join(", ", filled);
        }
    }

    internal sealed class TextSectionFormatter : ICreditsSectionFormatter
    {
        public void Format(CreditsSection section, CreditsFormatContext context, List<CreditsLine> output)
        {
            if (section.lines == null) return;

            foreach (string line in section.lines)
            {
                if (!string.IsNullOrWhiteSpace(line)) output.Add(new CreditsLine(CreditsLineStyle.Text, line.Trim()));
            }
        }
    }

    internal sealed class AssetsSectionFormatter : ICreditsSectionFormatter
    {
        public void Format(CreditsSection section, CreditsFormatContext context, List<CreditsLine> output)
        {
            if (section.entries == null) return;

            for (int i = 0; i < section.entries.Length; i++)
            {
                CreditsEntry entry = section.entries[i];
                if (entry == null) continue;

                string where = $"섹션 '{section.id}' entries[{i}]";
                string title = context.Require(entry.title, "title", "Untitled", where);
                string author = context.Require(entry.author, "author", "Unknown", where);

                // 모르는 라이선스는 표기가 빠지지 않도록 출처 표기가 필요한 것으로 취급합니다.
                string licenseName;
                bool attributionRequired = true;
                bool shareTextFileRequired = false;
                if (context.TryGetLicense(entry.licenseId, out CreditsLicense license))
                {
                    licenseName = string.IsNullOrWhiteSpace(license.displayName) ? license.id : license.displayName;
                    attributionRequired = license.attributionRequired;
                    shareTextFileRequired = license.shareTextFileRequired;
                }
                else if (string.IsNullOrWhiteSpace(entry.licenseId))
                {
                    context.Warn($"{where}의 licenseId 값이 비어 있습니다.");
                    licenseName = "Unknown License";
                }
                else
                {
                    context.Warn($"{where}의 licenseId '{entry.licenseId}'가 licenses 목록에 없습니다.");
                    licenseName = entry.licenseId.Trim();
                }

                string text = $"\"{title}\" by {author} — {licenseName}";
                if (attributionRequired)
                {
                    if (string.IsNullOrWhiteSpace(entry.sourceUrl))
                        context.Warn($"{where}는 출처 표기가 필요한데 sourceUrl 값이 비어 있습니다.");
                    else
                        text += $" ({entry.sourceUrl.Trim()})";

                    if (!string.IsNullOrWhiteSpace(entry.modification))
                        text += $", modified: {entry.modification.Trim()}";
                }

                string note = shareTextFileRequired ? CreditsFormatter.LicenseFileNote : null;
                output.Add(new CreditsLine(CreditsLineStyle.Asset, text, null, note));
            }
        }
    }
}
