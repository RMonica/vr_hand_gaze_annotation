using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PointCloudExporter
{
    public class PointCloudGenerator : MonoBehaviour
    {
        [Header("Point Cloud")]
        public string pcFileName = "airplane_20_annotated_normals";
        public string preloadAnnotationFileName = "";
        public float color_importance = 0.0F;
        public float normal_importance = 0.0F;
        public float position_importance = 0.0F;
        public float max_distance = 0.0F;

        //public int maximumVertices = 100000;

        [Header("Renderer")]
        public float sphere_size = 0.1f;
        public float disc_size = 0.1f;
        public float scale = 1f;
        public float big_sphere_size = 0.02f;
        public float selection_counter_alpha = 0.1f;
        public Texture sprite;
        public Shader shader;

        private const int verticesMax = 1000000; // up to 2^32 - 1 vertices with 32 bits mesh 
        private Material material;
        private Mesh[] meshArray;
        private Transform[] transformArray;
        private const string rviz_dll = "rviz_cloud_annotation_plugin";

        public bool controller_raycast_active;
        public bool eye_tracking_active;

        public List<GameObject> spheres = new List<GameObject>();
        private Dictionary<int, GameObject> big_spheres = new Dictionary<int, GameObject>();
        public GameObject pointCloudSpherePrefab;
        public GameObject pointCloudGameObject;
        public GameObject surfelCloudGameObject;
        public MenuController canvasMenuController;
        public ModeController modeController;

        public Material OnHoverActiveMaterial;
        public Material OnSelectedActiveMaterial;
        public Material OnPassiveStateMaterial;

        public float selection_cone_angle_eye_tracking = 2.0f;    // degrees
        public float selection_cone_radius_eye_tracking = 0.03f;  // m
        public float selection_cone_angle_controller = 2.0f;      // degrees
        public float selection_cone_radius_controller = 0.01f;    // m

        private float selection_cone_angle
        {
            get
            {
                if (modeController.activeMode == ActiveMode.EyeTracking)
                    return selection_cone_angle_eye_tracking;
                if (modeController.activeMode == ActiveMode.Controller)
                    return selection_cone_angle_controller;
                return 0.01f;
            }
        }
        private float selection_cone_radius
        {
            get
            {
                if (modeController.activeMode == ActiveMode.EyeTracking)
                    return selection_cone_radius_eye_tracking;
                if (modeController.activeMode == ActiveMode.Controller)
                    return selection_cone_radius_controller;
                return 0.01f;
            }
        }

        public float selection_time = 3.0f;         // seconds

        public int data_slot_i = -1;

        private float startingTime = 0.0f;
        private int lastSaveTime = 0;
        private int undo_count = 0;
        private int redo_count = 0;
        private int point_cloud_rotations_count = 0;
        private int bounding_box_overwrite_count = 0;
        private int bounding_box_rotations_count = 0;

        private bool A_pressed = false;
        private bool X_pressed = false;
        private bool Y_pressed = false;

        PointCloudEyeInteractable pcei;
        bool isActive = false;


        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rviz_cloud_annotation_get_new_data_slot();
        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rviz_cloud_annotation_free_data_slot(int data_i);

        //[DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        //private static extern int rviz_cloud_annotation_plugin_test(int a, int b);

        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rviz_cloud_annotation_loadcloud(int data_i, byte[] filename, float color_importance, float normal_importance, float position_importance, float max_distance);

        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rviz_cloud_annotation_savecloud(int data_i, byte[] filename, float[] color_list);

        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rviz_cloud_annotation_load(int data_i, byte[] filename);

        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rviz_cloud_annotation_save(int data_i, byte[] filename);

        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rviz_cloud_annotation_set_controlpoint(int data_i, [Out] int[] results, int point_id, int label, int weight);

        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rviz_cloud_annotation_set_controlpoint_vector(int data_i, [Out] int[] results, int[] point_ids, int count, int label, int weight);

        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rviz_cloud_annotation_get_labelforpoint(int data_i, int point_id);

        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rviz_cloud_annotation_getlabel_controlpointlist(int data_i, [Out] int[] results);

        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rviz_cloud_annotation_getweight_controlpointlist(int data_i, [Out] int[] results);

        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rviz_cloud_annotation_getlabel_controlpointforpoint(int data_i, int point_id);

        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rviz_cloud_annotation_getweight_controlpointforpoint(int data_i, int point_id);

        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rviz_cloud_annotation_get_nameforlabel(int data_i, [Out] byte[] str, int label);

        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rviz_cloud_annotation_get_labelpointcount(int data_i, int label);

        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int rviz_cloud_annotation_get_cloudsize(int data_i);

        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool rviz_cloud_annotation_get_cloud_points(int data_i, [Out] float[] points, int start_point, int end_point);
        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool rviz_cloud_annotation_get_cloud_normals(int data_i, [Out] float[] normals, int start_point, int end_point);
        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool rviz_cloud_annotation_get_cloud_colors(int data_i, [Out] float[] colors, int start_point, int end_point);
        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rviz_cloud_annotation_find_points_close_to_line(int data_i, float[] line_origin, float[] line_direction, float radius, int max_points,
                                                                                  [Out] int[] point_indices, [Out] float[] point_distances_along_ray, [Out] float[] point_distances_from_ray);
        private int rviz_cloud_annotation_find_points_close_to_line(Vector3 line_origin, Vector3 line_direction, float radius, int max_points,
                                                                    int[] point_indices, float[] point_distances_along_ray, float[] point_distances_from_ray)
        {
            float[] line_origin_arr = new float[3];
            line_origin_arr[0] = line_origin.x;
            line_origin_arr[1] = line_origin.y;
            line_origin_arr[2] = -line_origin.z; // moved from left-handed reference to right-handed reference

            float[] line_direction_arr = new float[3];
            line_direction_arr[0] = line_direction.x;
            line_direction_arr[1] = line_direction.y;
            line_direction_arr[2] = -line_direction.z; // moved from left-handed reference to right-handed reference
            return rviz_cloud_annotation_find_points_close_to_line(data_slot_i, line_origin_arr, line_direction_arr, radius, max_points,
                                                                    point_indices, point_distances_along_ray, point_distances_from_ray);
        }

        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rviz_cloud_annotation_find_points_in_box(int data_i, float[] box_min_a, float[] box_max_a, float[] box_pose_a, int max_points, [Out] int[] point_indices);
        public List<int> find_points_in_box(Vector3 box_min, Vector3 box_max, Matrix4x4 box_pose)
        {
            Matrix4x4 scale_matrix = Matrix4x4.identity;
            scale_matrix[0, 0] = 1.0f / scale;
            scale_matrix[1, 1] = 1.0f / scale;
            scale_matrix[2, 2] = -1.0f / scale; // left-handed reference frame to right-handed

            Matrix4x4 cloud_pose = pointCloudGameObject.transform.worldToLocalMatrix;
            Matrix4x4 local_box_pose = scale_matrix * cloud_pose * box_pose;

            float[] box_pose_a = new float[16];
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    box_pose_a[x + y * 4] = local_box_pose[y, x];

            float[] box_min_a = new float[3];
            box_min_a[0] = box_min[0];
            box_min_a[1] = box_min[1];
            box_min_a[2] = box_min[2];

            float[] box_max_a = new float[3];
            box_max_a[0] = box_max[0];
            box_max_a[1] = box_max[1];
            box_max_a[2] = box_max[2];

            int max_points = rviz_cloud_annotation_get_cloudsize(data_slot_i);
            int[] point_indices = new int[max_points];

            int count = rviz_cloud_annotation_find_points_in_box(data_slot_i, box_min_a, box_max_a, box_pose_a, max_points, point_indices);

            List<int> result = point_indices.ToList<int>();
            if (count < max_points)
                result.RemoveRange(count, max_points - count);

            return result;
        }

        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rviz_cloud_annotation_undo(int data_i, [Out] int[] results);

        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int rviz_cloud_annotation_redo(int data_i, [Out] int[] results);

        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool rviz_cloud_annotation_is_undo_enabled(int data_i);

        [DllImport(rviz_dll, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool rviz_cloud_annotation_is_redo_enabled(int data_i);

        public void setActive(bool active)
        {
            isActive = active;
        }

        public bool use_surfels = false;
        public PointCloudInfo.CloudInfo point_cloud_info;

        private Color[] passive_colors;
        private Color32[] mesh_colors_cache;
        private bool mesh_colors_cache_changed = false;
        private Dictionary<int, float> sparse_hover_mask = new Dictionary<int, float>();
        private int sparse_hover_mask_clear_counter; // if cloud not intercepted for five frames, hover mask is cleared
        private float[] selection_counters;
        private float[] selection_timers;
        private float[] vec;

        void Start()
        {
            data_slot_i = rviz_cloud_annotation_get_new_data_slot();
            Debug.Log("Start data_slot_i " + data_slot_i.ToString());

            GameObject canvas = GameObject.Find("Sample Canvas");
            if (!canvas)
            {
                Debug.Log("(PointCloudSphere) ERROR: CANVAS NOT FOUND");
                return;
            }
            else
            {
                canvasMenuController = canvas.GetComponent<MenuController>();
            }

            // point cloud selection
            if(modeController.selectedScene == SelectedScene.Airplane)
            {
                pcFileName = "airplane_20_annotated_normals";
                
                color_importance = 1.0f;
                normal_importance = 0.5f;
                position_importance = 1.0f;
                max_distance = 1.0f;

                sphere_size = 0.02f;
                disc_size = 0.02f;
                scale = 0.5f;
                big_sphere_size = 0.015f;
            }
            else if (modeController.selectedScene == SelectedScene.Workbench || modeController.selectedScene == SelectedScene.WorkbenchGT)
            {
                pcFileName = "workbench";
                if (modeController.selectedScene == SelectedScene.WorkbenchGT)
                    pcFileName = "ground_truth/ground_truth_" + pcFileName;

                color_importance = 1.0f;
                normal_importance = 0.5f;
                position_importance = 1.0f;
                max_distance = 1.0f;

                sphere_size = 0.005f;
                disc_size = 0.005f;
                scale = 1f;
                big_sphere_size = 0.01f;
            }
            else if (modeController.selectedScene == SelectedScene.Cone || modeController.selectedScene == SelectedScene.ConeGT)
            {
                pcFileName = "cone";
                if (modeController.selectedScene == SelectedScene.ConeGT)
                    pcFileName = "ground_truth/ground_truth_" + pcFileName;

                color_importance = 1.0f;
                normal_importance = 0.5f;
                position_importance = 1.0f;
                max_distance = 1.0f;

                sphere_size = 0.005f;
                disc_size = 0.005f;
                scale = 2f;
                big_sphere_size = 0.03f;
            }


            //test c++ integration
            //int test_plugin = rviz_cloud_annotation_plugin_test(3, 4);
            //Debug.Log("Test: " + test_plugin.ToString());
            //Debug.Log(" EYE TRACKING " + this.eye_tracking_active);
            Generate();
        }

        private void OnDestroy()
        {
            Debug.Log("OnDestroy data_slot_i " + data_slot_i.ToString());
            if (data_slot_i >= 0)
                rviz_cloud_annotation_free_data_slot(data_slot_i);
            data_slot_i = -1;
        }

        public PointCloudInfo.CloudInfo LoadPointCloud()
        {
            //C++ Loadcloud
            Debug.Log("LoadPointCloud for data_slot_i " + data_slot_i.ToString() + " filename " + pcFileName);
            string filePathcpp = System.IO.Path.Combine("Assets/Scripts/PointCloudImport/pointclouds/", pcFileName) + ".pcd\0";
            //Debug.Log("Load Cloud | Path: " + filePathcpp);
            byte[] filePathChar = Encoding.ASCII.GetBytes(filePathcpp);
            int cpp_interface_result = 0;
            cpp_interface_result = rviz_cloud_annotation_loadcloud(data_slot_i, filePathChar, color_importance, normal_importance, position_importance, max_distance);
            Debug.Log("Load Cloud | Result: " + cpp_interface_result.ToString());

            if (preloadAnnotationFileName != "")
            {
                if (preloadAnnotationFileName[0] == '"')
                    preloadAnnotationFileName = preloadAnnotationFileName.Substring(1);
                if (preloadAnnotationFileName[preloadAnnotationFileName.Length - 1] == '"')
                    preloadAnnotationFileName = preloadAnnotationFileName.Substring(0, preloadAnnotationFileName.Length - 1);


                Debug.Log("Preload Annotation | Loading file: '" + preloadAnnotationFileName + "'");
                byte[] fileNameChar = Encoding.ASCII.GetBytes(preloadAnnotationFileName);
                cpp_interface_result = rviz_cloud_annotation_load(data_slot_i, fileNameChar);
                Debug.Log("Preload Annotation | Result: " + cpp_interface_result.ToString());
            }

            //C++ CloudSize
            cpp_interface_result = rviz_cloud_annotation_get_cloudsize(data_slot_i);
            Debug.Log("Cloud Size | Size: " + cpp_interface_result.ToString());

            PointCloudInfo.CloudInfo cloud = new PointCloudInfo.CloudInfo();
            int cloud_size = rviz_cloud_annotation_get_cloudsize(data_slot_i);
            float[] cloud_points = new float[cloud_size * 3];
            float[] cloud_colors = new float[cloud_size * 3];
            float[] cloud_normals = new float[cloud_size * 3];
            bool points_ok = true;
            points_ok = points_ok && rviz_cloud_annotation_get_cloud_points(data_slot_i, cloud_points, 0, cloud_size);
            points_ok = points_ok && rviz_cloud_annotation_get_cloud_colors(data_slot_i, cloud_colors, 0, cloud_size);
            points_ok = points_ok && rviz_cloud_annotation_get_cloud_normals(data_slot_i, cloud_normals, 0, cloud_size);
            if (!points_ok)
                Debug.Log("Failed to get cloud points! ");
            cloud.vertices = new Vector3[cloud_size];
            cloud.normals = new Vector3[cloud_size];
            cloud.colors = new Color[cloud_size];
            cloud.point_size = sphere_size;
            cloud.scale = scale;
            for (int i = 0; i < cloud_size; i++)
            {
                cloud.vertices[i].x = cloud_points[i * 3 + 0];
                cloud.vertices[i].y = cloud_points[i * 3 + 1];
                cloud.vertices[i].z = -cloud_points[i * 3 + 2]; // moved from left-handed reference to right-handed reference

                cloud.normals[i].x = cloud_normals[i * 3 + 0];
                cloud.normals[i].y = cloud_normals[i * 3 + 1];
                cloud.normals[i].z = -cloud_normals[i * 3 + 2]; // moved from left-handed reference to right-handed reference

                cloud.colors[i].r = cloud_colors[i * 3 + 0];
                cloud.colors[i].g = cloud_colors[i * 3 + 1];
                cloud.colors[i].b = cloud_colors[i * 3 + 2];
                cloud.colors[i].a = 1.0f;
            }

            return cloud;
        }

        public PointCloudInfo.CloudInfo Generate()
        {
            material = new Material(shader);
            material.SetFloat("_Size", disc_size * scale);
            material.SetTexture("_MainTex", sprite);
            PointCloudInfo.CloudInfo points = LoadPointCloud();
            GenerateSurfelCloud(points, material, MeshTopology.Points);
            point_cloud_info = points;

            Debug.Log("points num: " + point_cloud_info.vertices.Length);

            passive_colors = new Color[points.colors.Length];
            for (int i = 0; i < passive_colors.Length; i++)
            {
                point_cloud_info.colors[i].a = 0.2f;
                passive_colors[i] = point_cloud_info.colors[i]; // Color.white;
            }

            UpdateCloudView();
            selection_counters = new float[points.colors.Length];
            Debug.Log("Dimensione " + points.colors.Length);
            selection_timers = new float[points.colors.Length];
            vec = new float[points.colors.Length];

            if (modeController.activeMode == ActiveMode.EyeTracking || modeController.activeMode == ActiveMode.Controller)
            {
                pcei = pointCloudGameObject.AddComponent<PointCloudEyeInteractable>();
                pcei.point_cloud_generator = this;
                pcei.mode_controller = modeController;
            }

            return points;
        }

        public void GenerateSurfelCloud(PointCloudInfo.CloudInfo meshInfo, Material materialToApply, MeshTopology topology)
        {
            for (int c = transform.childCount - 1; c >= 0; --c)
            {
                Transform child = transform.GetChild(c);
                if (child.gameObject.tag != "container")
                    GameObject.DestroyImmediate(child.gameObject);
            }

            int vertexCount = meshInfo.vertices.Length;
            int meshCount = (int)Mathf.Ceil(vertexCount / (float)verticesMax);
            meshArray = new Mesh[meshCount];
            transformArray = new Transform[meshCount];

            int index = 0;
            int meshIndex = 0;
            int vertexIndex = 0;

            int resolution = GetNearestPowerOfTwo(Mathf.Sqrt(vertexCount));
            while (meshIndex < meshCount)
            {
                Debug.Log("GenerateSurfelCloud while " + meshIndex.ToString());
                int count = verticesMax;
                if (vertexCount <= verticesMax)
                {
                    count = vertexCount;
                }
                else if (vertexCount > verticesMax && meshCount == meshIndex + 1)
                {
                    count = vertexCount % verticesMax;
                }

                Vector3[] subVertices = meshInfo.vertices.Skip(meshIndex * verticesMax).Take(count).Select(v => v * meshInfo.scale).ToArray();
                Vector3[] subNormals = meshInfo.normals.Skip(meshIndex * verticesMax).Take(count).ToArray();
                Color[] subColors = meshInfo.colors.Skip(meshIndex * verticesMax).Take(count).ToArray();

                int[] subIndices = new int[count];
                for (int i = 0; i < count; ++i)
                {
                    subIndices[i] = i;
                }

                Mesh mesh = new Mesh();
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // 32 bits mesh, DO NOT MODIFY
                mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100f);
                mesh.vertices = subVertices;
                mesh.normals = subNormals;
                mesh.colors = subColors;
                mesh.SetIndices(subIndices, topology, 0);

                Vector2[] uvs2 = new Vector2[mesh.vertices.Length];
                for (int i = 0; i < uvs2.Length; ++i)
                {
                    float x = vertexIndex % resolution;
                    float y = Mathf.Floor(vertexIndex / (float)resolution);
                    uvs2[i] = new Vector2(x, y) / (float)resolution;
                    ++vertexIndex;
                }
                mesh.uv2 = uvs2;

                //creating an airplane with mesh
                if (use_surfels)
                {
                    Vector4[] uv1 = new Vector4[mesh.vertices.Length];
                    for (int i = 0; i < uv1.Length; ++i)
                        uv1[i] = subColors[i];
                    mesh.SetUVs(1, uv1);

                    surfelCloudGameObject = CreateGameObjectWithMesh(mesh, materialToApply, gameObject.name + "_" + meshIndex, pointCloudGameObject.transform);
                    mesh_colors_cache = surfelCloudGameObject.GetComponent<MeshFilter>().mesh.colors32;
                    mesh_colors_cache_changed = false;
                }

                //meshArray[meshIndex] = mesh;
                //transformArray[meshIndex] = go.transform;

                index += count;
                ++meshIndex;
            }
            Debug.Log("GenerateSurfelCloud while end");

            if (!use_surfels)
                CreateSpheres(meshInfo, scale, sphere_size * scale);
            CreateCollider(meshInfo, scale);
        }

        private Dictionary<Color, Material> point_material_cache = new Dictionary<Color, Material>();
        public Material GetMaterialWithColor(Color color)
        {
            if (!point_material_cache.ContainsKey(color))
            {
                Material new_material = new Material(OnPassiveStateMaterial);
                new_material.color = color;
                point_material_cache.Add(color, new_material);
            }
            return point_material_cache[color];
        }

        public void SetPointColor(int idx, Color color)
        {
            if (!use_surfels)
            {
                GameObject sphere = spheres[idx];
                if (sphere.GetComponent<MeshRenderer>().material.color != color)
                    sphere.GetComponent<MeshRenderer>().material = GetMaterialWithColor(color);
            }
            else // use_surfels
            {
                if (mesh_colors_cache[idx] != color)
                {
                    mesh_colors_cache[idx] = color;
                    mesh_colors_cache_changed = true;
                }
            }

            if (big_spheres.ContainsKey(idx))
            {
                big_spheres[idx].GetComponent<MeshRenderer>().material = GetMaterialWithColor(color);
            }
        }

        public Color hover_color = new Color(1.0f, 0.5f, 0.0f, 1.0f);
        public Color max_hover_color = new Color(1.0f, 0.0f, 0.0f, 1.0f);

        public void SetPointHoverColor(int idx, float weight) { SetPointColor(idx, hover_color * (1.0f - weight) + max_hover_color * weight); }
        public void SetPointPassiveColor(int idx) { SetPointColor(idx, passive_colors[idx]); }

        public void OnPointHover(int idx)
        {
            ClearHoverMask();
            SetPointHoverColor(idx, 0.0f);
        }

        public void OnPointPassive(int idx)
        {
            SetPointPassiveColor(idx);
        }

        public void ClearHoverMask()
        {
            foreach (var v in sparse_hover_mask)
            {
                SetPointPassiveColor(v.Key);
            }
            sparse_hover_mask.Clear();
        }

        public void OnInterceptingEyeRay(Vector3 hovered_ray_origin, Vector3 hovered_ray_direction)
        {
            ClearHoverMask();
            List<WeightedPointIndex> hovered_points = FindWeightedPointsCloseToLine(hovered_ray_origin, hovered_ray_direction);

            int current_label = canvasMenuController.colorlabel;
            bool is_deleting = (current_label == 0);

            bool big_sphere_only = false;

            if (is_deleting)
            {
                float closest_big_sphere_dist = 0.0f;
                // check big spheres first since they have priority

                foreach (WeightedPointIndex wpi in hovered_points)
                {
                    if (!big_spheres.ContainsKey(wpi.idx))
                        continue;
                    if (!big_sphere_only)
                    {
                        big_sphere_only = true;
                        closest_big_sphere_dist = wpi.distance_along_line;
                    }
                    else
                        closest_big_sphere_dist = Math.Min(wpi.distance_along_line, closest_big_sphere_dist);
                }
                // check if there is a normal point much closer than the sphere
                foreach (WeightedPointIndex wpi in hovered_points)
                {
                    if (!big_spheres.ContainsKey(wpi.idx) &&
                        (wpi.distance_along_line < closest_big_sphere_dist - (big_sphere_size * 2.0f)))
                    {
                        big_sphere_only = false;
                        break;
                    }
                }
            }

            int current_max_score_point = -1;
            float current_max_score = 0.0f;
            {
                for (int i = 0; i < selection_counters.Length; i++)
                {
                    if (selection_counters[i] > current_max_score)
                    {
                        current_max_score = selection_counters[i];
                        current_max_score_point = i;
                    }
                }
            }
            if (current_max_score == 0.0f)
                current_max_score = 1.0f; // prevent division by zero

            foreach (WeightedPointIndex wpi in hovered_points)
            {
                if (big_sphere_only && !big_spheres.ContainsKey(wpi.idx))
                    continue;
                SetPointHoverColor(wpi.idx, selection_counters[wpi.idx] / current_max_score);
                sparse_hover_mask[wpi.idx] = wpi.w;
            }
        }

        private void UpdateSelectionCounters()
        {
            if (selection_counters == null)
                return;

            int current_label = canvasMenuController.colorlabel;
            bool is_deleting = (current_label == 0);

            // this variable stores the value of the physics fixed rate updates in selection_time seconds
            float selection_counter = selection_time / Time.fixedDeltaTime;


            foreach (KeyValuePair<int, float> hover_entry in sparse_hover_mask)
            {
                int hover_key = hover_entry.Key;
                float hover_value = hover_entry.Value;
                float new_value = 0.0f;
                if (!is_deleting && hover_value > 0.0f)
                {
                    new_value = hover_value;
                }
                // if it is deleting, increment only the control point spheres
                if (is_deleting && hover_value > 0.0f && big_spheres.ContainsKey(hover_key))
                {
                    new_value = hover_value; //1.0f;
                }

                // exponential running average
                selection_counters[hover_key] = selection_counters[hover_key] * (1.0f - selection_counter_alpha) + new_value * selection_counter_alpha;
            }

            //for (int i = 0; i < selection_counters.Length; i++)
            //    if (selection_counters[i] > 0.0f)
            //        selection_timers[i] += Time.fixedDeltaTime;

            if (A_pressed && ((modeController.activeMode == ActiveMode.Controller) || (modeController.activeMode == ActiveMode.EyeTracking)))
            {
                A_pressed = false;
                if (isActive)
                {
                    float max = 0;
                    int pos = 0;
                    for (int i = 0; i < selection_counters.Length; i++)
                    {
                        if (selection_counters[i] > max)
                        {
                            max = selection_counters[i];
                            pos = i;
                        }
                    }
                    /*
                    Debug.Log("Sfera Cliccata!");
                    Debug.Log("Colore scelto " + current_label);
                    Debug.Log("Posizione " + pos);
                    Debug.Log("Valore " + max);
                    */
                    Debug.Log("Point annotated with label: " + current_label);

                    SetControlPoint(pos, 10);

                    for (int i = 0; i < selection_counters.Length; i++)
                    {
                        selection_counters[i] = 0.0f;
                    }
                    //Debug.Log("Valore massimo dopo cancellazione " + selection_counters.Max());
                }
            }

            // UNDO
            if (X_pressed)
            {
                X_pressed = false;
                UndoControlPoint();
            }
            // REDO
            if (Y_pressed)
            {
                Y_pressed = false;
                RedoControlPoint();
            }

            //if(modeController.activeMode == ActiveMode.EyeTracking)
            //{
            //    for (int i = 0; i < selection_counters.Length; i++)
            //    {
            //        if (selection_counters[i] >= selection_counter/* && selection_timers[i] >= selection_time*/)
            //        {
            //            Debug.Log("Sfera Cliccata!!");
            //            Debug.Log("Colore scelto " + current_label);
            //            Debug.Log("Posizione " + i);

            //            SetControlPoint(i, 10);

            //            for (int i2 = 0; i2 < selection_counters.Length; i2++)
            //                selection_counters[i2] = 0.0f;
            //            for (int i2 = 0; i2 < selection_timers.Length; i2++)
            //                selection_timers[i2] = 0.0f;
            //        }
            //    }
            //}

            for (int i = 0; i < selection_counters.Length; i++)
            {
                if (!sparse_hover_mask.ContainsKey(i) || (is_deleting && !big_spheres.ContainsKey(i)))
                {
                    selection_counters[i] = 0.0f;
                    //selection_counters[i] = Math.Max(selection_counters[i] - 1.0f, 0.0f);
                    //if (selection_counters[i] == 0.0f)
                    //selection_timers[i] = 0.0f;
                }
            }
        }

        private void FixedUpdate()
        {
            UpdateSelectionCounters();
            isActive = false;
        }

        private void SaveFiles(int deltaTime, bool manual)
        {
            const String savePath = "C:\\Users\\RIMLab\\Documents\\SavedAnnotations";
            Debug.Log("Elapsed time: " + deltaTime + "s");

            // save annotation to files (pcd with point cloud data, annotation with library data, stats with statistic data)
            string filename = "";
            filename += "[" + DateTime.Now.ToString().Replace("/", "-").Replace(":", "_") + "]_" + modeController.userCode + "_" +
                        modeController.selectedScene.ToString() + "_" + modeController.activeMode.ToString() + "_" + deltaTime + "s";
            if (manual) filename += "_manual";

            // annotation data
            string filePathcpp = System.IO.Path.Combine(savePath, filename) + ".annotation\0";
            Debug.Log("Saving file " + filePathcpp);
            byte[] filePathChar = Encoding.ASCII.GetBytes(filePathcpp);
            int cpp_interface_save = rviz_cloud_annotation_save(data_slot_i, filePathChar);
            Debug.Log("File .annotation saved");

            // point cloud data
            filePathcpp = System.IO.Path.Combine(savePath, filename) + ".pcd\0";
            Debug.Log("Saving file " + filePathcpp);
            filePathChar = Encoding.ASCII.GetBytes(filePathcpp);

            // generate color list, using 1 float value for every color component of every color
            // add 1 color to account for white, used for unlabeled points
            float[] color_list = new float[(canvasMenuController.colorlist.Count + 1) * 3];
            color_list[0 + 0] = Color.white.r;
            color_list[0 + 1] = Color.white.g;
            color_list[0 + 2] = Color.white.b;

            for (int i = 0; i < canvasMenuController.colorlist.Count; i++)
            {
                color_list[((i + 1) * 3) + 0] = canvasMenuController.colorlist[i].r;
                color_list[((i + 1) * 3) + 1] = canvasMenuController.colorlist[i].g;
                color_list[((i + 1) * 3) + 2] = canvasMenuController.colorlist[i].b;
            }

            int cpp_interface_savecloud = rviz_cloud_annotation_savecloud(data_slot_i, filePathChar, color_list);
            Debug.Log("File .pcd saved");

            // statistics data
            filePathcpp = System.IO.Path.Combine(savePath, filename) + ".stats";
            Debug.Log("Saving file " + filePathcpp);
            using (StreamWriter outputFile = new StreamWriter(/*"./" + */filePathcpp))
            {
                outputFile.WriteLine("userCode," + modeController.userCode);
                outputFile.WriteLine("scene," + modeController.selectedScene.ToString());
                outputFile.WriteLine("mode," + modeController.activeMode.ToString());
                outputFile.WriteLine("elapsed_time," + deltaTime);
                outputFile.WriteLine("manual," + manual.ToString());

                if (modeController.activeMode == ActiveMode.Controller || modeController.activeMode == ActiveMode.EyeTracking)
                {
                    outputFile.WriteLine("undo_count," + undo_count);
                    outputFile.WriteLine("redo_count," + redo_count);
                    outputFile.WriteLine("point_cloud_rotations_count," + point_cloud_rotations_count);
                }

                if (modeController.activeMode == ActiveMode.BoxAnnotation)
                {
                    outputFile.WriteLine("bounding_box_overwrite_count," + bounding_box_overwrite_count);
                    outputFile.WriteLine("bounding_box_rotations_count," + bounding_box_rotations_count);
                }
            }
            Debug.Log("File .stats saved");
        }

        private void Update()
        {
            int deltaTime = (int)(Time.time - startingTime);
            // autosave point cloud every 30 seconds
            if ((startingTime != 0.0f) && (deltaTime % 30 == 0) && deltaTime > lastSaveTime)
            {
                SaveFiles(deltaTime, false);
                lastSaveTime = deltaTime;
            }

            //// time count
            if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch))
            {
                if (startingTime == 0.0f)
                {
                    startingTime = Time.time;
                    Debug.Log("Time started");
                }
                else
                {
                    SaveFiles(deltaTime, true);
                }
            }

            // UNDO
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch) && !X_pressed)
            {
                X_pressed = true;
            }
            // REDO
            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch) && !Y_pressed)
            {
                Y_pressed = true;
            }

            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch) && !A_pressed)
            {
                A_pressed = true;
            }

            if (use_surfels && mesh_colors_cache_changed)
            {
                MeshFilter mesh_filter = surfelCloudGameObject.GetComponent<MeshFilter>();
                Mesh mesh = mesh_filter.mesh;
                mesh.colors32 = mesh_colors_cache;
                mesh_colors_cache_changed = false;
            }
        }

        // http://stackoverflow.com/questions/466204/rounding-up-to-nearest-power-of-2
        public int GetNearestPowerOfTwo(float x)
        {
            return (int)Mathf.Pow(2f, Mathf.Ceil(Mathf.Log(x) / Mathf.Log(2f)));
        }

        public GameObject CreateGameObjectWithMesh(Mesh mesh, Material materialToApply, string name = "GeneratedMesh", Transform parent = null)
        {
            GameObject meshGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject.DestroyImmediate(meshGameObject.GetComponent<Collider>());
            meshGameObject.GetComponent<MeshFilter>().mesh = mesh;
            Debug.Log("mesh vertices length: " + mesh.vertices.Length);
            Debug.Log("mesh vertex count: " + mesh.vertexCount);
            meshGameObject.GetComponent<Renderer>().material = materialToApply;
            meshGameObject.GetComponent<Renderer>().material.color = Color.red;
            //meshGameObject.GetComponent<Renderer>().material.shader = shader;
            //meshGameObject.GetComponent<Renderer>().material.SetFloat("_Size", size * scale);
            //meshGameObject.GetComponent<Renderer>().material.SetTexture("_MainTex", sprite);
            //meshGameObject.GetComponent<Renderer>().material.SetPass(0);
            meshGameObject.name = name;
            meshGameObject.transform.parent = parent;
            meshGameObject.transform.localPosition = Vector3.zero;
            meshGameObject.transform.localRotation = Quaternion.identity;
            meshGameObject.transform.localScale = Vector3.one;
            return meshGameObject;
        }

        public void CreateCollider(PointCloudInfo.CloudInfo meshInfo, float scale)
        {
            float minX = Mathf.Infinity;
            float minY = Mathf.Infinity;
            float minZ = Mathf.Infinity;
            float maxX = -Mathf.Infinity;
            float maxY = -Mathf.Infinity;
            float maxZ = -Mathf.Infinity;

            Vector3[] vertices = meshInfo.vertices;

            foreach (var vert in vertices)
            {
                float positionX = vert.x * scale;
                float positionY = vert.y * scale;
                float positionZ = vert.z * scale;

                if (positionX > maxX) maxX = positionX;
                if (positionY > maxY) maxY = positionY;
                if (positionZ > maxZ) maxZ = positionZ;
                if (positionX < minX) minX = positionX;
                if (positionY < minY) minY = positionY;
                if (positionZ < minZ) minZ = positionZ;
            }

            pointCloudGameObject.GetComponent<PointCloudAABBController>().CreateAABB(maxX, maxY, maxZ, minX, minY, minZ, pointCloudGameObject.transform);
            pointCloudGameObject.GetComponent<Rigidbody>().isKinematic = false;
        }

        public void CreateSpheres(PointCloudInfo.CloudInfo meshInfo, float scale, float size)
        {
            Vector3[] vertices = meshInfo.vertices;

            // Create an index for the spheres to use it in c++ code later
            int index = 0;

            spheres.Clear();

            // Create the pointcloud
            foreach (var vert in vertices)
            {
                float positionX = vert.x * scale;
                float positionY = vert.y * scale;
                float positionZ = vert.z * scale;

                GameObject gameObject = Instantiate(pointCloudSpherePrefab, pointCloudGameObject.transform);

                gameObject.transform.localScale = new Vector3(size / 2, size / 2, size / 2);
                gameObject.transform.localPosition = new Vector3(positionX, positionY, positionZ);
                gameObject.transform.localRotation = Quaternion.identity;

                gameObject.layer = LayerMask.NameToLayer("PointCloudSphere");

                //gameObject.GetComponent<MeshRenderer>().enabled = false;

                gameObject.GetComponent<Collider>().isTrigger = true;
                if (gameObject.GetComponent<Rigidbody>() != null)
                    gameObject.GetComponent<Rigidbody>().isKinematic = true;

                // Make Sphere clickable
                // This class handles the click
                //PointCloudSphere sphere = gameObject.AddComponent<PointCloudSphere>();
                //sphere.id = index;
                //sphere.point_cloud_generator = this;
                //gameObject.AddComponent<Rigidbody>();
                //gameObject.GetComponent<Rigidbody>().isKinematic = true;
                if (use_surfels)
                    gameObject.GetComponent<MeshRenderer>().enabled = false;
                //if (eye_tracking_active == true)
                //{

                //    //gameObject.AddComponent<EyeInteractable>();
                //   // gameObject.GetComponent<EyeInteractable>().pointCloudTransform = pointCloudGameObject.transform;
                //}

                MeshRenderer mesh_renderer = gameObject.GetComponent<MeshRenderer>();
                mesh_renderer.material = new Material(OnPassiveStateMaterial);

                spheres.Add(gameObject);
                ++index;
            }

            Debug.Log("Spheres len: " + spheres.Count);
        }

        public void SetControlPointWithLabel(int point_id, int label_weight, int selected_label)
        {
            List<int> points = new List<int>();
            points.Add(point_id);
            SetControlPointWithLabel(points, label_weight, selected_label);
        }

        public void SetControlPoint(int point_id, int label_weight)
        {
            if (canvasMenuController == null)
            {
                Debug.Log("Could not read label: canvasMenuController not found.");
                return;
            }

            int selected_label = canvasMenuController.colorlabel;
            List<int> points = new List<int>();
            points.Add(point_id);

            SetControlPointWithLabel(points, label_weight, selected_label);
        }

        public void SetControlPointWithLabel(List<int> point_id, int label_weight, int selected_label)
        {
            int cloud_size = PointCloudGenerator.rviz_cloud_annotation_get_cloudsize(data_slot_i);
            int[] changed_control_points = new int[cloud_size];
            int count = point_id.Count;
            int[] point_ids = new int[count];
            for (int i = 0; i < count; i++)
            {
                point_ids[i] = point_id[i];
            }

            if (selected_label > 0)
            {
                int cpp_interface = PointCloudGenerator.rviz_cloud_annotation_set_controlpoint_vector(data_slot_i, changed_control_points, point_ids, count, selected_label, label_weight);
                //if (label_weight > 0)
                //    Debug.Log("Set Control Point | Result: " + cpp_interface.ToString() + " | Setted value " + selected_label.ToString() + " at index " + point_id.ToString());
            }
            else if (selected_label == 0)
            {
                int cpp_interface = PointCloudGenerator.rviz_cloud_annotation_set_controlpoint_vector(data_slot_i, changed_control_points, point_ids, count, selected_label, 0);
                //if (label_weight > 0)
                //    Debug.Log("Set Control Point | Result: " + cpp_interface.ToString() + " | Setted value " + selected_label.ToString() + " at index " + point_id.ToString());
            }

            UpdateCloudView();
        }

        public void UndoControlPoint()
        {
            if (PointCloudGenerator.rviz_cloud_annotation_is_undo_enabled(data_slot_i))
            {
                int cloud_size = PointCloudGenerator.rviz_cloud_annotation_get_cloudsize(data_slot_i);
                int[] changed_control_points = new int[cloud_size];

                int cpp_interface = PointCloudGenerator.rviz_cloud_annotation_undo(data_slot_i, changed_control_points);
                //Debug.Log("Undo Control Point | Result: " + cpp_interface.ToString());

                UpdateCloudView();

                undo_count++;
                Debug.Log("Undos incremented, current count: " + undo_count);
            }
        }

        public void RedoControlPoint()
        {
            if (PointCloudGenerator.rviz_cloud_annotation_is_redo_enabled(data_slot_i))
            {
                int cloud_size = PointCloudGenerator.rviz_cloud_annotation_get_cloudsize(data_slot_i);
                int[] changed_control_points = new int[cloud_size];

                int cpp_interface = PointCloudGenerator.rviz_cloud_annotation_redo(data_slot_i, changed_control_points);
                //Debug.Log("Redo Control Point | Result: " + cpp_interface.ToString());

                UpdateCloudView();

                redo_count++;
                Debug.Log("Redos incremented, current count: " + redo_count);
            }
        }

        public void OnCloudSelected(bool isit, Transform anchor = null)
        {
            if (!isit)
            {
                pointCloudGameObject.GetComponent<Rigidbody>().isKinematic = false;
                pointCloudGameObject.transform.SetParent(transform.parent);
            }
            else
            {
                pointCloudGameObject.GetComponent<Rigidbody>().isKinematic = true;
                pointCloudGameObject.transform.SetParent(anchor);
            }
        }

        public int FindPointCloseToLine(Vector3 line_origin, Vector3 line_direction)
        {
            Vector3 local_origin = pointCloudGameObject.transform.InverseTransformPoint(line_origin);
            local_origin = local_origin / scale;
            Vector3 local_direction = pointCloudGameObject.transform.InverseTransformDirection(line_direction);

            int[] point_indices = new int[1];
            float[] point_distances_along_line = new float[1];
            float[] point_distances_from_line = new float[1];
            int result = rviz_cloud_annotation_find_points_close_to_line(local_origin, local_direction, sphere_size / 2.0f, 1, point_indices, point_distances_along_line, point_distances_from_line);
            if (result == 0)
                return -1;
            return point_indices[0];
        }

        public struct WeightedPointIndex
        {
            public int idx;
            public float w;
            public float distance_along_line;
            public float distance_from_line;
        }

        public List<WeightedPointIndex> FindWeightedPointsCloseToLine(Vector3 line_origin, Vector3 line_direction)
        {
            Vector3 local_origin = pointCloudGameObject.transform.InverseTransformPoint(line_origin);
            local_origin = local_origin / scale;
            Vector3 local_direction = pointCloudGameObject.transform.InverseTransformDirection(line_direction);

            int max_pts = 1000;

            int[] point_indices = new int[max_pts];
            float[] point_distances_along_line = new float[max_pts];
            float[] point_distances_from_line = new float[max_pts];
            int num_pts = rviz_cloud_annotation_find_points_close_to_line(local_origin, local_direction, selection_cone_radius / scale, max_pts, point_indices, point_distances_along_line, point_distances_from_line);

            List<WeightedPointIndex> result = new List<WeightedPointIndex>();

            float tan_cone_angle = (float)Math.Tan((double)selection_cone_angle * Math.PI / 180.0);

            float distance_along_line_first_point = -1.0f; // distance of the closest point (points are returned in order)

            for (int i = 0; i < num_pts && i < max_pts; i++)
            {
                float distance_along_line = point_distances_along_line[i] * scale;
                float distance_from_line = point_distances_from_line[i] * scale;

                if (distance_along_line < 0.0f)
                    continue; // point is behind observer

                distance_along_line = Math.Max(distance_along_line, sphere_size * scale / 2.0f);

                if (distance_along_line_first_point < 0.0f)
                    distance_along_line_first_point = distance_along_line;

                float cone_threshold = distance_along_line * tan_cone_angle;
                cone_threshold = Math.Max(cone_threshold, sphere_size * scale / 2.0f); // at least the size of a sphere
                cone_threshold = Math.Min(cone_threshold, selection_cone_radius);      // at most max radius
                if (distance_from_line > cone_threshold)
                    continue; // point is out of cone

                float weight = ((cone_threshold - distance_from_line) / cone_threshold) * Math.Max(0.0f, 1.0f - (distance_along_line - distance_along_line_first_point) / (10.0f * sphere_size * scale));
                if (weight == 0.0f)
                    continue; // null weight

                WeightedPointIndex wpi = new WeightedPointIndex();
                wpi.idx = point_indices[i];
                wpi.w = weight;
                wpi.distance_along_line = distance_along_line;
                wpi.distance_from_line = distance_from_line;
                result.Add(wpi);
            }

            return result;
        }

        public void UpdateCloudView()
        {
            int cloud_size = rviz_cloud_annotation_get_cloudsize(data_slot_i);

            float size = sphere_size * scale / 2.0f;
            float big_size = Math.Max(size * 2.0f, big_sphere_size);

            for (int i = 0; i < cloud_size; i++)
            {
                int control_point_label = rviz_cloud_annotation_getlabel_controlpointforpoint(data_slot_i, i);
                int control_point_weight = rviz_cloud_annotation_getweight_controlpointforpoint(data_slot_i, i);
                if (control_point_label != 0 && control_point_weight != 0)
                {
                    if (!big_spheres.ContainsKey(i))
                    {
                        GameObject big_sphere = Instantiate(pointCloudSpherePrefab, pointCloudGameObject.transform);

                        big_sphere.transform.localScale = new Vector3(big_size, big_size, big_size);
                        big_sphere.transform.localPosition = point_cloud_info.vertices[i] * scale;
                        big_sphere.transform.localRotation = Quaternion.identity;

                        big_sphere.layer = LayerMask.NameToLayer("PointCloudSphere");

                        //PointCloudSphere sphere = big_sphere.AddComponent<PointCloudSphere>();
                        //sphere.id = i;
                        //sphere.point_cloud_generator = this;
                        //big_sphere.GetComponent<Rigidbody>().isKinematic = true;
                        //big_sphere.GetComponent<Collider>().isTrigger = true;

                        big_spheres.Add(i, big_sphere);
                    }
                }
                else
                {
                    if (big_spheres.ContainsKey(i))
                    {
                        Destroy(big_spheres[i]);
                        big_spheres.Remove(i);
                    }
                }

                int point_label = rviz_cloud_annotation_get_labelforpoint(data_slot_i, i);
                if (point_label > 0)
                {
                    passive_colors[i] = canvasMenuController.colorlist[point_label - 1];
                    SetPointPassiveColor(i);
                }
                else
                {
                    passive_colors[i] = Color.white; //point_cloud_info.colors[i]; // Color.white;
                    SetPointPassiveColor(i);
                }

            }

        }

        public void IncrementPointCloudRotationsCount()
        {
            point_cloud_rotations_count++;
            Debug.Log("Point cloud rotations incremented, current count: " + point_cloud_rotations_count);
        }

        public void IncrementBoundingBoxRotationsCount()
        {
            bounding_box_rotations_count++;
            Debug.Log("Bounding box rotations incremented, current count: " + bounding_box_rotations_count);
        }

        public void IncrementBoundingBoxOverwritesCount()
        {
            bounding_box_overwrite_count++;
            Debug.Log("Bounding box overwrites incremented, current count: " + bounding_box_overwrite_count);
        }
    }
}
