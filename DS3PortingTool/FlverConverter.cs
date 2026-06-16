using SoulsFormats;

namespace DS3PortingTool;

public static class FlverConverter
{
    private static bool TryConvert_0_To_2(BinderFile flverFile, int flver2Version)
    {
        FLVER0 flv0 = FLVER0.Read(flverFile.Bytes);
        FLVER2 flv2 = new FLVER2
        {
            Header = new FLVER2.FLVERHeader
            {
                BoundingBoxMin = flv0.Header.BoundingBoxMin,
                BoundingBoxMax = flv0.Header.BoundingBoxMax,
                Unicode =  flv0.Header.Unicode,
                Version = flver2Version
            },
            Dummies = flv0.Dummies,
            Nodes = flv0.Nodes,
        };

        foreach (FLVER0.Mesh flv0Mesh in flv0.Meshes)
        {
            FLVER2.Mesh flv2Mesh = new FLVER2.Mesh();
            FLVER2.FaceSet faceSet = new FLVER2.FaceSet();
            for (int i = 0; i < flv0Mesh.Vertices.Count; i++)
            {
                FLVER.Vertex v = flv0Mesh.Vertices[i];
                faceSet.Indices.Add(i);
            }
            flv2Mesh.FaceSets.Add(faceSet);
            flv2.Meshes.Add(flv2Mesh);
        }

        return true;
    }
}