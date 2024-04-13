namespace AdventureLandSharp.Core;

public static class MapGraphExporter
{
    public static string Export(MapGraph graph)
    {
        StringBuilder sb = new();

        sb.AppendLine("digraph G {");

        foreach (var group in graph.Vertices.GroupBy(v => v.Map.Name))
        {
            var colour = $"#{new Random().Next(0xFFFFFF):X6}";

            sb.AppendLine($"    subgraph cluster_{group.Key} {{");

            var verts = group.Where(graph.Edges.ContainsKey);

            foreach (var vertex in verts) sb.AppendLine($"        \"{vertex}\" [color=\"{colour}\"];");

            foreach (var edge in verts.SelectMany(v => graph.Edges[v]))
                sb.AppendLine($"        \"{edge.Source}\" -> \"{edge.Dest}\"");

            sb.AppendLine("    }");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }
}