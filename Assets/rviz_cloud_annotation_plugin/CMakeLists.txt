cmake_minimum_required(VERSION 3.8)
project(rviz_cloud_annotation_plugin)
cmake_policy(SET CMP0074 NEW)

# set(CMAKE_MODULE_PATH "${CMAKE_MODULE_PATH};C:/Program Files/PCL 1.13.1/cmake/Modules")

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

# set(CMAKE_TOOLCHAIN_FILE "${CMAKE_CURRENT_SOURCE_DIR}/vcpkg/scripts/buildsystems/vcpkg.cmake" CACHE STRING "Vcpkg toolchain file")

# set(Eigen_DIR "C:/Users/RIMLab/vcpkg/buildtrees/eigen3/x86-windows-rel")
# set(Eigen3_DIR "C:/Users/RIMLab/vcpkg/buildtrees/eigen3/x86-windows-rel")
# find_package(Eigen3 REQUIRED CONFIG)
# include_directories(${Eigen3_INCLUDE_DIRS})
# link_directories(${Eigen3_LIBRARY_DIRS})
# add_definitions(${Eigen3_DEFINITIONS})

# set(FLANN_DIR "C:/Users/RIMLab/vcpkg/buildtrees/flann/x86-windows-rel/generated")
# find_package(FLANN REQUIRED CONFIG)
# include_directories(${FLANN_INCLUDE_DIRS})
# link_directories(${FLANN_LIBRARY_DIRS})
# add_definitions(${FLANN_DEFINITIONS})

# set(PCL_DIR "C:/Users/RIMLab/vcpkg/buildtrees/pcl/x86-windows-rel")
# find_package(PCL REQUIRED CONFIG)
# include_directories(${PCL_INCLUDE_DIRS})
# link_directories(${PCL_LIBRARY_DIRS})
# add_definitions(${PCL_DEFINITIONS})

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


### COPY LIBRARIES ###
# set(RUNTIME_OUTPUT_DIRECTORY "C:/Users/RIMLab/Desktop/Bertuzzi/MovementSDKDemos/Assets/rviz_cloud_annotation_plugin/build/RelWithDebInfo")
# file(COPY ${PCL_LIBRARIES} DESTINATION ${RUNTIME_OUTPUT_DIRECTORY})
# file(COPY ${FLANN_LIBRARIES} DESTINATION ${RUNTIME_OUTPUT_DIRECTORY})
# file(COPY ${VTK_LIBRARIES} DESTINATION ${RUNTIME_OUTPUT_DIRECTORY})
# file(COPY ${Eigen3_LIBRARIES} DESTINATION ${RUNTIME_OUTPUT_DIRECTORY})

### LINK LIBRARIES TO TEST ###
target_link_libraries(rviz_cloud_annotation_test
  ${PCL_LIBRARIES}
  ${Eigen3_LIBRARIES}
  rviz_cloud_annotation_plugin
)

### DEBUGGING ###
# message(STATUS "FLANN_INCLUDE_DIR ${FLANN_INCLUDE_DIR}")
# message(STATUS "PCL_INCLUDE_DIRS ${PCL_INCLUDE_DIRS}")
# message(STATUS "CMAKE_MODULE_PATH ${CMAKE_MODULE_PATH}")
