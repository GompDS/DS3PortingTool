using System.Numerics;
using SoulsFormats;

namespace DS3PortingTool
{
    public static class VertexExtensions
    {
        public static void PadVertex(this FLVER.Vertex vertex, IEnumerable<FLVER2.BufferLayout> bufferLayouts)
        {
            Dictionary<FLVER.LayoutSemantic, int> usageCounts = new();
            FLVER.LayoutSemantic[] paddedProperties =
                {FLVER.LayoutSemantic.Tangent, FLVER.LayoutSemantic.UV, FLVER.LayoutSemantic.VertexColor};

            IEnumerable<FLVER.LayoutMember> layoutMembers = bufferLayouts.SelectMany(bufferLayout => bufferLayout)
                .Where(x => paddedProperties.Contains(x.Semantic));
            foreach (FLVER.LayoutMember layoutMember in layoutMembers)
            {
                bool isDouble = layoutMember.Semantic == FLVER.LayoutSemantic.UV &&
                                layoutMember.Type is FLVER.LayoutType.Float4 or FLVER.LayoutType.UVPair;
                int count = isDouble ? 2 : 1;
                    
                if (usageCounts.ContainsKey(layoutMember.Semantic))
                {
                    usageCounts[layoutMember.Semantic] += count;
                }
                else
                {
                    usageCounts.Add(layoutMember.Semantic, count);
                }
            }

            if (usageCounts.ContainsKey(FLVER.LayoutSemantic.Tangent))
            {
                int missingTangentCount = usageCounts[FLVER.LayoutSemantic.Tangent] - vertex.Tangents.Count;
                for (int i = 0; i < missingTangentCount; i++)
                {
                    vertex.Tangents.Add(Vector4.Zero);
                }
            }
            
            if (usageCounts.ContainsKey(FLVER.LayoutSemantic.UV))
            {
                int missingUvCount = usageCounts[FLVER.LayoutSemantic.UV] - vertex.UVs.Count;
                for (int i = 0; i < missingUvCount; i++)
                {
                    vertex.UVs.Add(Vector3.Zero);
                }
            }
            
            if (usageCounts.ContainsKey(FLVER.LayoutSemantic.VertexColor))
            {
                int missingColorCount = usageCounts[FLVER.LayoutSemantic.VertexColor] - vertex.Colors.Count;
                for (int i = 0; i < missingColorCount; i++)
                {
                    vertex.Colors.Add(new FLVER.VertexColor(255, 255, 0, 255));
                }
            }
        }
    }
}