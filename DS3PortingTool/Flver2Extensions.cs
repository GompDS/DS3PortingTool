using SoulsAssetPipeline.FLVERImporting;
using SoulsFormats;

namespace DS3PortingTool
{
    public static class Flver2Extensions
    {
        public static FLVER2 ToDs3Flver(this FLVER2 oldFlver)
        {
            FLVER2MaterialInfoBank materialInfoBank = FLVER2MaterialInfoBank.ReadFromXML($"{AppDomain.CurrentDomain.BaseDirectory}Res\\BANKDS3.xml");
            FLVER2 newFlver = new FLVER2()
            {
                Header = new FLVER2.FLVERHeader()
                {
                    BoundingBoxMin = oldFlver.Header.BoundingBoxMin,
                    BoundingBoxMax = oldFlver.Header.BoundingBoxMax
                },
                Dummies = oldFlver.Dummies,
                Materials = oldFlver.Materials.Select(x => x.ToDummyDs3Material()).ToList(),
                Bones = oldFlver.Bones,
                Meshes = oldFlver.Meshes,
                SekiroUnk = oldFlver.SekiroUnk
            };
            List<FLVER2.Material> distinctMaterials = newFlver.Materials.DistinctBy(x => x.MTD).ToList();
            foreach (var distinctMat in distinctMaterials)
            {
                FLVER2MaterialInfoBank.MaterialDef matDef = materialInfoBank.MaterialDefs.Values
                    .First(x => x.MTD.Equals($"{Path.GetFileName(distinctMat.MTD).ToLower()}"));
                // Buffer Layouts
                newFlver.BufferLayouts.AddRange(matDef.AcceptableVertexBufferDeclarations[0].Buffers);
                // GXLists
                FLVER2.GXList gxList = new FLVER2.GXList();
                gxList.AddRange(materialInfoBank.GetDefaultGXItemsForMTD(Path.GetFileName(distinctMat.MTD).ToLower()));
                newFlver.GXLists.Add(gxList);
                // Set material GXIndexes
                foreach (var mat in newFlver.Materials.Where(x => x.MTD.Equals(distinctMat.MTD)))
                {
                    mat.GXIndex = distinctMaterials.FindIndex(x => x.MTD.Equals(mat.MTD));
                }
            }
            List<int> layoutIndices = newFlver.GetLayoutIndices(newFlver.BufferLayouts);    
            foreach (var mesh in newFlver.Meshes)
            {
                mesh.Vertices = mesh.Vertices.Select(x =>
                {
                    x.PadVertex(newFlver.BufferLayouts);
                    return x;
                }).ToList();
                mesh.VertexBuffers = layoutIndices.Select(x => new FLVER2.VertexBuffer(x)).ToList();
                
            }
            return newFlver;
        }
        
        private static List<int> GetLayoutIndices(this FLVER2 flver, List<FLVER2.BufferLayout> bufferLayouts)
        {
            List<int> indices = new();

            foreach (FLVER2.BufferLayout referenceBufferLayout in bufferLayouts)
            {
                for (int i = 0; i < flver.BufferLayouts.Count; i++)
                {
                    FLVER2.BufferLayout bufferLayout = flver.BufferLayouts[i];
                    if (bufferLayout.Select(x => (x.Type, x.Semantic)).SequenceEqual(referenceBufferLayout.Select(x => (x.Type, x.Semantic))))
                    {
                        indices.Add(i);
                        break;
                    }

                    if (i == flver.BufferLayouts.Count - 1)
                    {
                        indices.Add(i + 1);
                        flver.BufferLayouts.Add(referenceBufferLayout);
                        break;
                    }
                }
            }

            return indices;
        }
    }   
}