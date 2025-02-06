using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PointCloudExporter;

public class PointCloudInfo : MonoBehaviour
{
    public struct CloudInfo
    {
        public Vector3[] vertices;
        public Vector3[] normals;
        public Color[] colors;
        public float scale;
        public float point_size;
    }
}
