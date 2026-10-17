namespace BattleRoyale.Maps
{
    public static class MapTitle
    {
        public static string From(MapDefinition map)
        {
            if (map == null)
            {
                return "Map";
            }

            var markdown = map.narrativeMarkdown == null
                ? null
                : map.narrativeMarkdown.text;
            return Heading(markdown) ?? Readable(map.name);
        }

        public static string Heading(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return null;
            }

            var start = 0;
            while (start < markdown.Length)
            {
                var end = markdown.IndexOf('\n', start);
                if (end < 0)
                {
                    end = markdown.Length;
                }

                var line = markdown.Substring(start, end - start).Trim().TrimEnd('\r');
                if (line.StartsWith("#"))
                {
                    var title = line.TrimStart('#').Trim();
                    return title.Length == 0 ? null : title;
                }

                if (line.Length > 0)
                {
                    return null;
                }

                start = end + 1;
            }

            return null;
        }

        public static string Readable(string assetName)
        {
            if (string.IsNullOrWhiteSpace(assetName))
            {
                return "Map";
            }

            var name = assetName.Trim();
            const string suffix = "_Map";
            if (name.EndsWith(suffix))
            {
                name = name.Substring(0, name.Length - suffix.Length);
            }

            var parts = name.Split('_');
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0)
                {
                    continue;
                }

                parts[i] = char.ToUpper(parts[i][0]) +
                    (parts[i].Length == 1 ? string.Empty : parts[i].Substring(1));
            }

            var text = string.Join(" ", parts).Trim();
            return text.Length == 0 ? "Map" : text;
        }
    }
}
