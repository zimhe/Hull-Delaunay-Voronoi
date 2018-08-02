using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HullDelaunayVoronoi.Delaunay;
using HullDelaunayVoronoi.Primitives;
using Random = UnityEngine.Random;

namespace HullDelaunayVoronoi
{

    public class ExampleDelaunay3D : MonoBehaviour
    {

        public int NumberOfVertices = 100;

        public float size = 5;

        public int seed = 0;

        private Material lineMaterial;

        private Matrix4x4 rotation = Matrix4x4.identity;

        [SerializeField] private GameObject _boundryTriangle;

        private Mesh _mesh;

        private float theta;

        private DelaunayTriangulation3 delaunay;
        private bool[] _cellLabels;

        private void Start()
        {
            lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));

            Vertex3[] vertices = new Vertex3[NumberOfVertices];

            Random.InitState(seed);
            for (int i = 0; i < NumberOfVertices; i++)
            {
                float x = size * Random.Range(-1.0f, 1.0f);
                float y = size * Random.Range(-1.0f, 1.0f);
                float z = size * Random.Range(-1.0f, 1.0f);

                vertices[i] = new Vertex3(x, y, z);
            }

            delaunay = new DelaunayTriangulation3();
            delaunay.Generate(vertices);

            // DEBUG
            //CheckVertices();
            //CheckCells();


            TagCells();
            _cellLabels = delaunay.Cells.Select(ShouldInclude).ToArray();

            //var tris = GetBoundaryTriangles().SelectMany(v => v);
            var tris = GetBoundaryTriangles();

            _mesh=new Mesh
            {
                //vertices = VertexToVector()


            };

            /*
            foreach (var tri in tris)
            {
                foreach (var v in tri)
                {
                    
                }
            }
            */
        }
        List<Vector3> VertexToVector(IList<Vertex3> v)
        {
            List<Vector3> vs = new List<Vector3>();
            foreach (var vertex3 in v)
            {
                vs.Add(new Vector3(vertex3.X, vertex3.Y, vertex3.Z));
            }

            return vs;
        }


        /// <summary>
        /// Tags cells by their position in the array
        /// </summary>
        private void TagCells()
        {
            var cells = delaunay.Cells;
            
            for (int i = 0; i < cells.Count; i++)
                cells[i].Simplex.Tag = i;
        }


        /// <summary>
        /// Returns true if the given cell is to be included
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        private bool ShouldInclude(DelaunayCell<Vertex3> cell)
        {
            // TODO check area and aspect ratio of cell
            return false;
        }

        
        /// <summary>
        /// Returns all vertices that exist in BOTH of the given arrays
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private IEnumerable<Vertex3> Intersection(Vertex3[] a, Vertex3[] b)
        {
            for (int i = 0; i < a.Length; i++)
                a[i].Tag = 0;

            for (int i = 0; i < b.Length; i++)
                b[i].Tag = 1;

            return a.Where(v => v.Tag == 1);
        }


        /// <summary>
        /// Returns triangles on boundary
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IEnumerable<Vertex3>> GetBoundaryTriangles()
        {
            var cells = delaunay.Cells;

            for (int i = 0; i < cells.Count; i++)
            {
                var s0 = cells[i].Simplex;

                foreach (var s1 in s0.Adjacent)
                {
                    if (s1 == null || _cellLabels[s0.Tag] == _cellLabels[s1.Tag]) continue;
                    yield return Intersection(s0.Vertices, s1.Vertices);
                }
            }
        }

    

        private void CheckVertices()
        {
            var verts = delaunay.Vertices;

            foreach (var v in verts)
            {
                Debug.Log(v.Id);
            }
        }


        private void CheckCells()
        {
            // check simplex tags
            var cells = delaunay.Cells;

            // tag cells
            for (int i = 0; i < cells.Count; i++)
                cells[i].Simplex.Tag = i;

            // check adjacency of a cell
            CheckCell(cells[100].Simplex);
        }


        private void CheckCell(Simplex<Vertex3> simplex)
        {
            Debug.Log($"Cell {simplex.Tag} is of dimension {simplex.Dimension}");
            Debug.Log($"It is adjacent to...");

            foreach (var adj in simplex.Adjacent)
            {
                if (adj == null)
                    Debug.Log($"the boundary");
                else
                    Debug.Log($"simplex {adj.Tag}");
            }
        }
        


        private void Update()
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
            {
                theta += (Input.GetKey(KeyCode.A)) ? 0.005f : -0.005f;

                rotation[0, 0] = Mathf.Cos(theta);
                rotation[0, 2] = Mathf.Sin(theta);
                rotation[2, 0] = -Mathf.Sin(theta);
                rotation[2, 2] = Mathf.Cos(theta);
            }
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(20, 20, Screen.width, Screen.height), "Numpad +/- to Rotate");
        }

        private void OnPostRender()
        {
            if (delaunay == null || delaunay.Cells.Count == 0 || delaunay.Vertices.Count == 0) return;

            GL.PushMatrix();

            GL.LoadIdentity();
            GL.MultMatrix(GetComponent<Camera>().worldToCameraMatrix * rotation);
            GL.LoadProjectionMatrix(GetComponent<Camera>().projectionMatrix);

            lineMaterial.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Color(Color.red);

            foreach (DelaunayCell<Vertex3> cell in delaunay.Cells)
            {
                DrawSimplex(cell.Simplex);
            }

            GL.End();
            GL.PopMatrix();
        }

        private void DrawSimplex(Simplex<Vertex3> f)
        {
            GL.Vertex3(f.Vertices[0].X, f.Vertices[0].Y, f.Vertices[0].Z);
            GL.Vertex3(f.Vertices[1].X, f.Vertices[1].Y,f.Vertices[1].Z);

            GL.Vertex3(f.Vertices[0].X, f.Vertices[0].Y, f.Vertices[0].Z);
            GL.Vertex3(f.Vertices[2].X, f.Vertices[2].Y, f.Vertices[2].Z);

            GL.Vertex3(f.Vertices[0].X, f.Vertices[0].Y, f.Vertices[0].Z);
            GL.Vertex3(f.Vertices[3].X, f.Vertices[3].Y, f.Vertices[3].Z);

            GL.Vertex3(f.Vertices[1].X, f.Vertices[1].Y, f.Vertices[1].Z);
            GL.Vertex3(f.Vertices[2].X, f.Vertices[2].Y, f.Vertices[2].Z);

            GL.Vertex3(f.Vertices[3].X, f.Vertices[3].Y, f.Vertices[3].Z);
            GL.Vertex3(f.Vertices[1].X, f.Vertices[1].Y, f.Vertices[1].Z);

            GL.Vertex3(f.Vertices[3].X, f.Vertices[3].Y, f.Vertices[3].Z);
            GL.Vertex3(f.Vertices[2].X, f.Vertices[2].Y, f.Vertices[2].Z);
        }


        /*
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private IEnumerable<DelaunayCell<Vertex3>> GetFilteredCells()
        {
            return delaunay.Cells.Where(Filter);
        }


        /// <summary>
        /// Return true if cell is ok
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        private bool Filter(DelaunayCell<Vertex3> cell)
        {
            // TODO check area and aspect ratio of cell
            return false;

            var verts = cell.Simplex;
            var adj = cell.Simplex.Adjacent; //
            var tag = cell.Simplex.Tag; // TODO check tags - see if sequential
        }
        */
    }

}



















