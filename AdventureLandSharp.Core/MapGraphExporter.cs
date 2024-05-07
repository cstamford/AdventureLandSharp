using System.Text;

namespace AdventureLandSharp.Core;

public static class MapGraphExporter {
    public static string Export(MapGraph graph) {
        StringBuilder sb = new();

        sb.AppendLine("digraph G {");

        /*
        foreach (IGrouping<string, MapLocation> group in graph.Vertices.GroupBy(v => v.Map.Name)) {
            string colour = $"#{new Random().Next(0xFFFFFF):X6}";

            sb.AppendLine($"    subgraph cluster_{group.Key} {{");

            IEnumerable<MapLocation> verts = group.Where(graph.Edges.ContainsKey);

            foreach (MapLocation vertex in verts) {
                sb.AppendLine($"        \"{vertex}\" [color=\"{colour}\"];");
            }

            foreach (IMapGraphEdge edge in verts.SelectMany(v => graph.Edges[v])) {
                sb.AppendLine($"        \"{edge.Source}\" -> \"{edge.Dest}\"");
            }

            sb.AppendLine("    }");
        }
        */

        sb.AppendLine("}");

        return sb.ToString();
    }
}