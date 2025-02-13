#include "rviz_cloud_annotation_c_interface.h"

#include "rviz_cloud_annotation_undo.h"
#include "point_neighborhood.h"
#include "rviz_cloud_annotation.h"

#include <memory>
#include <vector>

#include <pcl/point_cloud.h>
#include <pcl/point_types.h>
#include <pcl/io/pcd_io.h>
#include <pcl/kdtree/kdtree_flann.h>

typedef pcl::PointCloud<pcl::PointXYZRGBL> PointXYZRGBLCloud;
typedef pcl::PointCloud<pcl::PointXYZRGBNormal> PointXYZRGBNormalCloud;
typedef pcl::KdTreeFLANN<pcl::PointXYZRGBNormal> KdTree;
struct AnnotationData
{
    std::shared_ptr<RVizCloudAnnotationUndo> rviz_cloud_annotation_undo;

    std::shared_ptr<RVizCloudAnnotationPoints> rviz_cloud_annotation_points;

    std::shared_ptr<PointNeighborhood> point_neighborhood;

    std::shared_ptr<KdTree> kdtree;
    PointXYZRGBNormalCloud::Ptr point_cloud;

    PointNeighborhood::Conf conf;
    int weight_steps;
};
typedef std::shared_ptr<AnnotationData> AnnotationDataPtr;
typedef std::vector<AnnotationDataPtr> AnnotationDataPtrVector;

//GLOBAL VARIABLES
static AnnotationDataPtrVector global_annotation_data;
#define G (global_annotation_data[data_i])

extern "C"
{

EXPORT_API int rviz_cloud_annotation_get_new_data_slot()
{
    for (int i = 0; i < global_annotation_data.size(); i++)
        if (!global_annotation_data[i]) // already existing free slot
        {
            global_annotation_data[i].reset(new AnnotationData);
            return i;
        }

    global_annotation_data.push_back(AnnotationDataPtr(new AnnotationData));
    return global_annotation_data.size() - 1;
}
EXPORT_API int rviz_cloud_annotation_free_data_slot(const int data_i)
{
    if (data_i >= global_annotation_data.size())
        return -1;
    global_annotation_data[data_i].reset(); // delete and set to null
    return 0;
}

EXPORT_API int rviz_cloud_annotation_loadcloud(const int data_i, const char * const filename, const float color_importance, const float normal_importance, const float position_importance, const float max_distance)
{
  G->point_cloud.reset(new PointXYZRGBNormalCloud);
  if (pcl::io::loadPCDFile(std::string(filename), *G->point_cloud))
    return int(RvizCloudAnnotationError::FILE_NOT_FOUND);

  G->kdtree.reset(new KdTree);
  G->kdtree->setInputCloud(G->point_cloud);

  try
  {
      G->conf.searcher = PointNeighborhoodSearch::CreateFromString(PARAM_VALUE_NEIGH_SEARCH_KNN_ATMOST, "10");
  }
  catch (const PointNeighborhoodSearch::ParserException & )
  {
    return int(RvizCloudAnnotationError::UNKNOWN_SEARCHER);
  }

  G->conf.color_importance = color_importance; //PARAM_DEFAULT_COLOR_IMPORTANCE;
  G->conf.normal_importance = normal_importance; //PARAM_DEFAULT_NORMAL_IMPORTANCE;
  G->conf.position_importance = position_importance; //PARAM_DEFAULT_POSITION_IMPORTANCE;

  G->conf.max_distance = max_distance; //0.5;

  G->weight_steps = PARAM_DEFAULT_WEIGHT_STEPS;

  G->point_neighborhood.reset(new PointNeighborhood(G->point_cloud, G->conf));

  G->rviz_cloud_annotation_points.reset(new RVizCloudAnnotationPoints(G->point_cloud->size(), G->weight_steps, G->point_neighborhood));

  G->rviz_cloud_annotation_undo.reset(new RVizCloudAnnotationUndo);
  G->rviz_cloud_annotation_undo->SetAnnotation(G->rviz_cloud_annotation_points);

  return int(RvizCloudAnnotationError::NONE);
}

struct PointIndicesWithDistances
{
    int index;
    float distance_along_ray;
    float distance_from_ray;
};

EXPORT_API int rviz_cloud_annotation_find_points_close_to_line(const int data_i, const float* line_origin, const float* line_direction, const float radius, const int max_points,
                                                               int* point_indices, float* point_distances_along_ray, float* point_distances_from_ray)
{
    const Eigen::Vector3f origin(line_origin[0], line_origin[1], line_origin[2]);
    const Eigen::Vector3f direction(line_direction[0], line_direction[1], line_direction[2]);

    std::vector<PointIndicesWithDistances> result;

    for (int i = 0; i < G->point_cloud->size(); i++)
    {
        pcl::PointXYZRGBNormal pt = (*(G->point_cloud))[i];
        Eigen::Vector3f ept(pt.x, pt.y, pt.z);

        float distance_from_ray = ((ept - origin) - ((ept - origin).dot(direction) * direction)).norm();
        float distance_along_ray = (ept - origin).dot(direction);
        if (distance_from_ray < radius)
        {
            PointIndicesWithDistances piwd;
            piwd.index = i;
            piwd.distance_along_ray = distance_along_ray;
            piwd.distance_from_ray = distance_from_ray;
            result.push_back(piwd);
        }
    }

    std::sort(result.begin(), result.end(), [](const PointIndicesWithDistances& a, const PointIndicesWithDistances& b) {
        if (a.distance_along_ray < 0.0f && b.distance_along_ray > 0.0f)
            return false; // sort < 0 later
        if (a.distance_along_ray > 0.0f && b.distance_along_ray < 0.0f)
            return true; // sort > 0 first
        return std::abs(a.distance_along_ray) < std::abs(b.distance_along_ray);
        });

    for (int i = 0; i < result.size() && i < max_points; i++)
    {
        point_indices[i] = result[i].index;
        point_distances_along_ray[i] = result[i].distance_along_ray;
        point_distances_from_ray[i] = result[i].distance_from_ray;
    }

    return std::min<int>(result.size(), max_points);
}

EXPORT_API int rviz_cloud_annotation_find_points_in_box(const int data_i, const float* box_min_a, const float* box_max_a, const float* box_pose_a, const int max_points, int* point_indices)
{
    const Eigen::Vector3f box_min(box_min_a[0], box_min_a[1], box_min_a[2]);
    const Eigen::Vector3f box_max(box_max_a[0], box_max_a[1], box_max_a[2]);
    Eigen::Matrix4f box_pose;
    for (int y = 0; y < 4; y++)
        for (int x = 0; x < 4; x++)
            box_pose(y, x) = box_pose_a[x + y * 4];
    const Eigen::Matrix4f box_pose_inv = box_pose.inverse();

    std::vector<int> result;

    for (int i = 0; i < G->point_cloud->size(); i++)
    {
        pcl::PointXYZRGBNormal pt = (*(G->point_cloud))[i];
        Eigen::Vector4f ept(pt.x, pt.y, pt.z, 1.0f);
        Eigen::Vector4f tpt = box_pose_inv * ept;
        Eigen::Vector3f tpt3 = tpt.head<3>() / tpt.w();
        if ((tpt3.array() < box_max.array()).all() && (tpt3.array() >= box_min.array()).all())
            result.push_back(i);
    }

    for (int i = 0; i < result.size() && i < max_points; i++)
    {
        point_indices[i] = result[i];
    }

    return std::min<int>(result.size(), max_points);
}

EXPORT_API int rviz_cloud_annotation_savecloud(const int data_i, const char* const filename, const float* const color_list)
{
    /*
    if (pcl::io::savePCDFile(std::string(filename), *(G->point_cloud)))
        return int(RvizCloudAnnotationError::FILE_NOT_FOUND);

    return int(RvizCloudAnnotationError::NONE);
    */

    // this point cloud has only the spatial information from the original one, the color is taken from the label of each point
    // NOTE: we use the same mapping (label_id -> color) used in Unity, which is passed as a parameter to this function
    PointXYZRGBLCloud result_cloud;
    pcl::copyPointCloud(*(G->point_cloud), result_cloud);
    G->rviz_cloud_annotation_points->LabelCloudWithColor(result_cloud);
    // use the mapping to color the points in the point cloud
    const int cloud_size = result_cloud.size();
    for (int i = 0; i < cloud_size; i++) {
        int label_id = result_cloud[i].label;
        result_cloud[i].r = (uint8_t) (color_list[(label_id * 3) + 0] * 255.0f);
        result_cloud[i].g = (uint8_t) (color_list[(label_id * 3) + 1] * 255.0f);
        result_cloud[i].b = (uint8_t) (color_list[(label_id * 3) + 2] * 255.0f);
    }

    /*
    // this point cloud has all the informations from the original one, plus the annotation label, inserted in the curvature field
    PointXYZRGBNormalCloud total_cloud_out;
    pcl::copyPointCloud(*(G->point_cloud), total_cloud_out);

    // copy annotations from 'label' field to 'curvature' field
    for (int i = 0; i < cloud_size; i++) {
        total_cloud_out[i].curvature = (float) result_cloud[i].label;
    }

    if (pcl::io::savePCDFileBinary(std::string(filename), total_cloud_out))
        return int(RvizCloudAnnotationError::FILE_NOT_FOUND);
    
    if (pcl::io::savePCDFileBinary(std::string(filename).replace(std::string(filename).find(".pcd"), 4, "_colored_annotation.pcd"), result_cloud))
        return int(RvizCloudAnnotationError::FILE_NOT_FOUND);
    */

    if (pcl::io::savePCDFileBinary(std::string(filename), result_cloud))
        return int(RvizCloudAnnotationError::FILE_NOT_FOUND);

    return int(RvizCloudAnnotationError::NONE);
}

EXPORT_API int rviz_cloud_annotation_load(const int data_i, const char * const filename)
{
    G->weight_steps = PARAM_DEFAULT_WEIGHT_STEPS;

    {
        std::ifstream istr(filename, std::ios::binary);
        if (!istr)
            return int(RvizCloudAnnotationError::FILE_NOT_FOUND);

        bool load_error = false;
        try
        {
            G->rviz_cloud_annotation_points = RVizCloudAnnotationPoints::Deserialize(istr, G->weight_steps, G->point_neighborhood);

            G->rviz_cloud_annotation_undo.reset(new RVizCloudAnnotationUndo);
            G->rviz_cloud_annotation_undo->SetAnnotation(G->rviz_cloud_annotation_points);
        }
        catch (const RVizCloudAnnotationPoints::IOE e)
        {
            load_error = true;
        }

        if (!load_error)
            return int(RvizCloudAnnotationError::NONE);
    }

    std::ifstream istr(filename, std::ios::binary);
    if (!istr)
        return int(RvizCloudAnnotationError::FILE_NOT_FOUND);

    // file was saved in text mode, desperate attempt at fixing it :(
    std::string uncorrupted;
    char prev_b = 0;
    istr.read(&prev_b, 1);
    char b;
    while (istr.read(&b, 1))
    {
        if (prev_b == '\r' && b == '\n')
        {
            prev_b = b;
        }
        else
        {
            uncorrupted += prev_b;
            prev_b = b;
        }
    }
    uncorrupted += prev_b;
    std::istringstream iss(uncorrupted);
    
    {
        try
        {
            G->rviz_cloud_annotation_points = RVizCloudAnnotationPoints::Deserialize(iss, G->weight_steps, G->point_neighborhood);

            G->rviz_cloud_annotation_undo.reset(new RVizCloudAnnotationUndo);
            G->rviz_cloud_annotation_undo->SetAnnotation(G->rviz_cloud_annotation_points);
        }
        catch (const RVizCloudAnnotationPoints::IOE e)
        {
            return int(RvizCloudAnnotationError::FILE_LOAD_ERROR);
        }
    }
    
    return int(RvizCloudAnnotationError::NONE);
}

EXPORT_API int rviz_cloud_annotation_save(const int data_i, const char * const filename)
{
    //std::ostringstream ostr(filename);
    std::ofstream ostr(filename, std::ios::binary);

    try
    {
        G->rviz_cloud_annotation_points->Serialize(ostr);
    }
    catch (const RVizCloudAnnotationPoints::IOE e)
    {
        return int(RvizCloudAnnotationError::FILE_NOT_FOUND);
    }

    return int(RvizCloudAnnotationError::NONE);
}

EXPORT_API int rviz_cloud_annotation_set_controlpoint(const int data_i, int *results, const int point_id, const int label, const int weight)
{
#ifdef USE_UNDO
    RVizCloudAnnotationUndo::Uint64Vector temp = G->rviz_cloud_annotation_undo->SetControlPoint(point_id, weight, label);
#else
    RVizCloudAnnotationPoints::Uint64Vector temp = G->rviz_cloud_annotation_points->SetControlPoint(point_id, weight, label);
#endif

    for (int i = 0; i < temp.size(); ++i)
    {
        results[i] = temp[i];
    }
    return int(RvizCloudAnnotationError::NONE);
}

EXPORT_API int rviz_cloud_annotation_set_controlpoint_vector(const int data_i, int * results, const int * const point_ids, const int count, const int label, const int weight)
{ 
    RVizCloudAnnotationUndo::Uint64Vector point_ids_v(count);
    for (int i = 0; i < count; i++)
        point_ids_v[i] = point_ids[i];

#ifdef USE_UNDO
    RVizCloudAnnotationUndo::Uint64Vector temp = G->rviz_cloud_annotation_undo->SetControlPointVector(point_ids_v, weight, label);
#else
    RVizCloudAnnotationPoints::Uint64Vector temp = G->rviz_cloud_annotation_points->SetControlPointVector(data_i, weight, label);
#endif

    for (int i = 0; i < temp.size(); ++i)
    {
        results[i] = temp[i];
    }
    return int(RvizCloudAnnotationError::NONE);
}

EXPORT_API int rviz_cloud_annotation_get_labelforpoint(const int data_i, const int point_id)
{
    //CHECK FOR ERROR?

    //ACTIVELY RETURNS SOMETHING
    return G->rviz_cloud_annotation_points->GetLabelForPoint(point_id);
}

EXPORT_API int rviz_cloud_annotation_getlabel_controlpointlist(const int data_i, int *results)
{
    RVizCloudAnnotationPoints::CPDataVector temp = G->rviz_cloud_annotation_points->GetControlPointList(0);
    
    //CHECK FOR ERROR?
    for (int i = 0; i < temp.size(); ++i)
    {
        results[i] = -1;
    }
    for (int i = 0; i < temp.size(); ++i)
    {
        results[temp[i].point_id] = temp[i].label_id;
    }

    return int(RvizCloudAnnotationError::NONE);
}

EXPORT_API int rviz_cloud_annotation_getweight_controlpointlist(const int data_i, int* results)
{
    RVizCloudAnnotationPoints::CPDataVector temp = G->rviz_cloud_annotation_points->GetControlPointList(0);

    //CHECK FOR ERROR?
    for (int i = 0; i < temp.size(); ++i)
    {
        results[i] = -1;
    }
    for (int i = 0; i < temp.size(); ++i)
    {
        results[temp[i].point_id] = temp[i].weight_step_id;
    }

    return int(RvizCloudAnnotationError::NONE);
}

EXPORT_API int rviz_cloud_annotation_clear(const int data_i, int* results)
{
#ifdef USE_UNDO
    // the annotation undo clear already calls the annotation points clear, no need to do it twice
    RVizCloudAnnotationUndo::Uint64Vector temp = G->rviz_cloud_annotation_undo->Clear();
#else
    RVizCloudAnnotationPoints::Uint64Vector temp = G->rviz_cloud_annotation_points->Clear();
#endif
    //CHECK FOR ERROR?
    
    for (int i = 0; i < temp.size(); ++i)
    {
        results[i] = temp[i];
    }

    return int(RvizCloudAnnotationError::NONE);
}

EXPORT_API int rviz_cloud_annotation_set_nameforlabel(const int data_i, int* results, const int label, const char* name)
{
#ifdef USE_UNDO
    RVizCloudAnnotationUndo::Uint64Vector temp = G->rviz_cloud_annotation_undo->SetNameForLabel(label, name);
#else
    RVizCloudAnnotationPoints::Uint64Vector temp = G->rviz_cloud_annotation_points->SetNameForLabel(label, name);
#endif
    
    //CHECK FOR ERROR?
    
    for (int i = 0; i < temp.size(); ++i)
    {
        results[i] = temp[i];
    }

    return int(RvizCloudAnnotationError::NONE);
}

EXPORT_API int rviz_cloud_annotation_get_labelpointlist(const int data_i, int* results, const int label)
{
    RVizCloudAnnotationPoints::Uint64Vector temp = G->rviz_cloud_annotation_points->GetLabelPointList(label);
    
    //CHECK FOR ERROR?
    
    for (int i = 0; i < temp.size(); ++i)
    {
        results[i] = temp[i];
    }

    return int(RvizCloudAnnotationError::NONE);
}

EXPORT_API int  rviz_cloud_annotation_getlabel_controlpointforpoint(const int data_i, const int point_id)
{
    RVizCloudAnnotationPoints::CPData temp = G->rviz_cloud_annotation_points->GetControlPointForPoint(point_id);
    
    //CHECK FOR ERROR?
    
    // ACTIVELY RETURNS SOMETHING
    return temp.label_id;
}

EXPORT_API int  rviz_cloud_annotation_getweight_controlpointforpoint(const int data_i, const int point_id)
{
    RVizCloudAnnotationPoints::CPData temp = G->rviz_cloud_annotation_points->GetControlPointForPoint(point_id);

    //CHECK FOR ERROR?

    // ACTIVELY RETURNS SOMETHING
    return temp.weight_step_id;
}

EXPORT_API int rviz_cloud_annotation_get_nameforlabel(const int data_i, char* str, const int label)
{
    std::string tempstr = G->rviz_cloud_annotation_points->GetNameForLabel(label);

    const char* temp = tempstr.c_str();
    
    //CHECK FOR ERROR?
    
    int dim = tempstr.size();
    
    //CHECK FOR ERROR?
    
    for (int i = 0; i < dim; ++i)
    {
        str[i] = temp[i];
    }

    return int(RvizCloudAnnotationError::NONE);
}

EXPORT_API int rviz_cloud_annotation_get_labelpointcount(const int data_i, const int label)
{
    //CHECK FOR ERROR?
    // ACTIVELY RETURNS SOMETHING
    return G->rviz_cloud_annotation_points->GetLabelPointCount(label);
}

EXPORT_API int rviz_cloud_annotation_get_cloudsize(const int data_i)
{
    return G->rviz_cloud_annotation_points->GetCloudSize();
}

EXPORT_API bool rviz_cloud_annotation_get_cloud_points(const int data_i, float* points, const int start_point, const int end_point)
{
    if (!G->point_cloud)
        return false;
    if (start_point < 0)
        return false;

    const int cloud_size = G->point_cloud->size();

    PointXYZRGBNormalCloud::Ptr point_cloud = G->point_cloud;

    for (int i = start_point; i < end_point; i++)
    {
        if (i >= cloud_size)
            return false;

        points[(i - start_point) * 3 + 0] = (*point_cloud)[i].x;
        points[(i - start_point) * 3 + 1] = (*point_cloud)[i].y;
        points[(i - start_point) * 3 + 2] = (*point_cloud)[i].z;
    }

    return true;
}

EXPORT_API bool rviz_cloud_annotation_get_cloud_normals(const int data_i, float* normals, const int start_point, const int end_point)
{
    if (!G->point_cloud)
        return false;
    if (start_point < 0)
        return false;

    PointXYZRGBNormalCloud::Ptr point_cloud = G->point_cloud;

    const int cloud_size = point_cloud->size();

    for (int i = start_point; i < end_point; i++)
    {
        if (i >= cloud_size)
            return false;

        normals[(i - start_point) * 3 + 0] = (*point_cloud)[i].normal_x;
        normals[(i - start_point) * 3 + 1] = (*point_cloud)[i].normal_y;
        normals[(i - start_point) * 3 + 2] = (*point_cloud)[i].normal_z;
    }

    return true;
}

EXPORT_API bool rviz_cloud_annotation_get_cloud_colors(const int data_i, float* colors, const int start_point, const int end_point)
{
    if (!G->point_cloud)
        return false;
    if (start_point < 0)
        return false;

    PointXYZRGBNormalCloud::Ptr point_cloud = G->point_cloud;

    const int cloud_size = point_cloud->size();

    for (int i = start_point; i < end_point; i++)
    {
        if (i >= cloud_size)
            return false;

        colors[(i - start_point) * 3 + 0] = (*point_cloud)[i].r / 255.0f;
        colors[(i - start_point) * 3 + 1] = (*point_cloud)[i].g / 255.0f;
        colors[(i - start_point) * 3 + 2] = (*point_cloud)[i].b / 255.0f;
    }

    return true;
}

#ifdef USE_UNDO

EXPORT_API int rviz_cloud_annotation_undo(const int data_i, int* results)
{
    RVizCloudAnnotationUndo::Uint64Vector temp = G->rviz_cloud_annotation_undo->Undo();

    //CHECK FOR ERROR?

    for (int i = 0; i < temp.size(); ++i)
    {
        results[i] = temp[i];
    }
    return int(RvizCloudAnnotationError::NONE);
}

EXPORT_API int rviz_cloud_annotation_redo(const int data_i, int* results)
{
    RVizCloudAnnotationUndo::Uint64Vector temp = G->rviz_cloud_annotation_undo->Redo();

    //CHECK FOR ERROR?

    for (int i = 0; i < temp.size(); ++i)
    {
        results[i] = temp[i];
    }
    return int(RvizCloudAnnotationError::NONE);
}

EXPORT_API bool rviz_cloud_annotation_is_undo_enabled(const int data_i)
{
    return G->rviz_cloud_annotation_undo->IsUndoEnabled();
}

EXPORT_API bool rviz_cloud_annotation_is_redo_enabled(const int data_i)
{
    return G->rviz_cloud_annotation_undo->IsRedoEnabled();
}

#endif

}
