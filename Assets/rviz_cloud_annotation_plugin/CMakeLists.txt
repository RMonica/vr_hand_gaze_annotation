cmake_minimum_required(VERSION 3.8)
project(rviz_cloud_annotation_plugin)
cmake_policy(SET CMP0074 NEW)

### LIBRARY ###
add_library(rviz_cloud_annotation_plugin SHARED
  src/point_neighborhood.cpp
  src/point_neighborhood_search.cpp
  src/rviz_cloud_annotation_c_interface.cpp
  src/rviz_cloud_annotation_point_plane.cpp
  src/rviz_cloud_annotation_points.cpp
  src/rviz_cloud_annotation_points_io.cpp
  src/rviz_cloud_annotation_undo.cpp
  src/rviz_cloud_annotation_plugin.cpp
  )

### MAIN EXECUTABLE ###
add_executable(rviz_cloud_annotation_test
  src/rviz_cloud_annotation_test.cpp
)

set(PCL_DIR "C:/Program Files/PCL 1.13.1/cmake")
find_package(PCL REQUIRED CONFIG)
include_directories(${PCL_INCLUDE_DIRS})
link_directories(${PCL_LIBRARY_DIRS})
add_definitions(${PCL_DEFINITIONS})

find_package(FLANN REQUIRED)
include_directories(${FLANN_INCLUDE_DIR})


### LINK LIBRARIES ###
target_link_libraries(rviz_cloud_annotation_plugin 
  ${PCL_LIBRARIES}
  ${FLANN_LIBRARIES})

### LINK LIBRARIES TO TEST ###
target_link_libraries(rviz_cloud_annotation_test
  ${PCL_LIBRARIES}
  ${Eigen3_LIBRARIES}
  rviz_cloud_annotation_plugin
)
