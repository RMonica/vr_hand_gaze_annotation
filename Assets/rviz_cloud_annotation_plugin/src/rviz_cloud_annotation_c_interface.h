
#if _MSC_VER // this is defined when compiling with Visual Studio
  #define EXPORT_API __declspec(dllexport) // Visual Studio needs annotating exported functions with this
#else
  #define EXPORT_API
#endif

// define macro to use undo/redo logic
#define USE_UNDO

enum class RvizCloudAnnotationError
{
  NONE           = 0,
  FILE_NOT_FOUND = 1,
  UNKNOWN_SEARCHER = 2,
  OTHER_ERROR    = 3,
  FILE_LOAD_ERROR = 4,
};

struct CPData
{
	int point_id;
	int weight_step_id;
	int label_id;

	CPData(int pi, int wsi, int li) : point_id(pi), weight_step_id(wsi), label_id(li) {}
	CPData() : point_id(0), weight_step_id(0), label_id(0) {}
};

extern "C"
{
// gets a new data slot, which can be passed as data_i
EXPORT_API int rviz_cloud_annotation_get_new_data_slot();
// frees a data slot, to be reused
EXPORT_API int rviz_cloud_annotation_free_data_slot(const int data_i);

	//loads a pointcloud given a filename
EXPORT_API int rviz_cloud_annotation_loadcloud(const int data_i, const char * const filename, const float color_importance, const float normal_importance, const float position_importance, const float max_distance);
	//loads a pointcloud given a filename
EXPORT_API int rviz_cloud_annotation_savecloud(const int data_i, const char* const filename, const float* const color_list);
	//laods an annotation given a filename
EXPORT_API int rviz_cloud_annotation_load(const int data_i, const char * const filename);
	//saves an annotation given a filename
EXPORT_API int rviz_cloud_annotation_save(const int data_i, const char * const filename);
	//sets control points, given an index and a label
EXPORT_API int rviz_cloud_annotation_set_controlpoint(const int data_i, int* results, const int point_id, const int label, const int weight);
    //sets an array of count control points, given the indices and a label
EXPORT_API int rviz_cloud_annotation_set_controlpoint_vector(const int data_i, int* results, const int* const point_ids, const int count, const int label, const int weight);
	//given an index, gets the label of the point, 0 if none
EXPORT_API int rviz_cloud_annotation_get_labelforpoint(const int data_i, const int point_id);
	//gets list of labels of control points, sets -1 for points that are not control points
EXPORT_API int rviz_cloud_annotation_getlabel_controlpointlist(const int data_i, int* results);
	//gets list of weight of control points, sets -1 for points that are not control points
EXPORT_API int rviz_cloud_annotation_getweight_controlpointlist(const int data_i, int* results);
	//clear
EXPORT_API int rviz_cloud_annotation_clear(const int data_i, int* results);
	//sets name for a label and modifies label list
EXPORT_API int rviz_cloud_annotation_set_nameforlabel(const int data_i, int* results, const int label, const char* name);
	//given a label, returns list of pointn indexes with equal label
EXPORT_API int rviz_cloud_annotation_get_labelpointlist(const int data_i, int* results, const int label);
	//given a point id, returns its label
EXPORT_API int rviz_cloud_annotation_getlabel_controlpointforpoint(const int data_i, const int point_id);
	//given a point id, returns its weight
EXPORT_API int rviz_cloud_annotation_getweight_controlpointforpoint(const int data_i, const int point_id);
	//given label, returns name
EXPORT_API int rviz_cloud_annotation_get_nameforlabel(const int data_i, char* str, const int label);
	//given a label returns number of point labeled with it
EXPORT_API int rviz_cloud_annotation_get_labelpointcount(const int data_i, const int label);
	//gets size of the pointcloud
EXPORT_API int rviz_cloud_annotation_get_cloudsize(const int data_i);
    //gets a subset of points from the point cloud
    // as array of floats [x1, y1, z1, x2, y2, z2] ...
EXPORT_API bool rviz_cloud_annotation_get_cloud_points(const int data_i, float* points, const int start_point, const int end_point);
EXPORT_API bool rviz_cloud_annotation_get_cloud_normals(const int data_i, float* normals, const int start_point, const int end_point);
EXPORT_API bool rviz_cloud_annotation_get_cloud_colors(const int data_i, float* colors, const int start_point, const int end_point);

    // finds points close to line within radius, returns number of points found
EXPORT_API int rviz_cloud_annotation_find_points_close_to_line(const int data_i, const float * line_origin, const float * line_direction, const float radius, const int max_points,
	                                                            int * point_indices, float * point_distances_along_ray, float * point_distances_from_ray);
    // finds points in bounding box [box_min, box_max] with pose [box_pose]
EXPORT_API int rviz_cloud_annotation_find_points_in_box(const int data_i, const float* box_min, const float* box_max, const float* box_pose, const int max_points, int* point_indices);
#ifdef USE_UNDO
EXPORT_API int rviz_cloud_annotation_undo(const int data_i, int* results);
EXPORT_API int rviz_cloud_annotation_redo(const int data_i, int* results);
EXPORT_API bool rviz_cloud_annotation_is_undo_enabled(const int data_i);
EXPORT_API bool rviz_cloud_annotation_is_redo_enabled(const int data_i);
#endif
}

//TODO
/*
*	- dato un punto della pointcloud, dire se è di controllo, se lo è di che tipo di etichetta è
*		p1: non c'è una funzione che ritorna questa cosa
* 


	x Uint64Vector SetControlPoint(const uint64 point_id,const uint32 weight_step,const uint64 label);
	- Uint64Vector SetControlPointVector(const Uint64Vector & ids,
                                const Uint32Vector & weight_steps,
                                const Uint64Vector & labels);
	- Uint64Vector SetControlPoint(const CPData & control_point_data);
	- Uint64Vector SetControlPointList(const CPDataVector & control_points_data,const uint64 label);
	- Uint64Vector SetControlPointList(const CPDataVector & control_points_data);

	x Uint64Vector Clear();
	- Uint64Vector ClearLabel(const uint64 label); // clear label, returns list of affected labels
	x Uint64Vector SetNameForLabel(const uint64 label,const std::string & name);

	x CPDataVector GetControlPointList(const uint64 label) const;
	x Uint64Vector GetLabelPointList(const uint64 label) const;

	- uint64 GetWeightStepsCount() const {return m_weight_steps_count; }

	x uint64 GetLabelForPoint(const uint64 idx) const

	x CPData GetControlPointForPoint(const uint64 idx) const

	x std::string GetNameForLabel(const uint64 label) const

	- uint64 GetNextLabel() const {return m_control_points_for_label.size() + 1; }
	- uint64 GetMaxLabel() const {return m_control_points_for_label.size(); }
	- uint64 GetCloudSize() const {return m_cloud_size; }

	x uint64 GetLabelPointCount(const uint64 label) const
*/