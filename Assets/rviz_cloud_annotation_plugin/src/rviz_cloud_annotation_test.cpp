#include "rviz_cloud_annotation_c_interface.h"

#include <iostream>

int main(int argc, char ** argv)
{
  if (argc < 1)
  {
    std::cout << "Usage: rviz_cloud_annotation_test cloud.pcd" << std::endl;
    std::exit(1);
  }

  int slot = rviz_cloud_annotation_get_new_data_slot();


  // Test loadcloud
  std::string cloud_filenamein = "airplane_20_annotated_normals.pcd";
  std::cout << "rviz_cloud_annotation_test: Loading cloud " << cloud_filenamein << std::endl;

  int result = rviz_cloud_annotation_loadcloud(slot, cloud_filenamein.c_str(), 0.0, 0.0, 1.0, 0.5);

  std::cout << "Test loadcloud | Result is " << result << std::endl;


  //// Test savecloud
  //std::string cloud_filenameout = "TESTCLOUDOUT.pcd";
  //result = rviz_cloud_annotation_savecloud(slot, cloud_filenameout.c_str());

  //std::cout << "Test savecloud | Result is " << result << std::endl;


  // Test get_cloudsize
  int const cloudsize = rviz_cloud_annotation_get_cloudsize(slot);

  std::cout << "Test get_cloudsize | Size is " << cloudsize << std::endl;

  int* cplist = (int*) malloc(sizeof(int) * cloudsize);
  for (int i = 0; i < cloudsize; ++i)
  {
      cplist[i] = 31212012;
  }


  // FILENAME
  // Test load
  // std::string annotation_filenamein = "TESTANNOTATIONIN";
  // result = rviz_cloud_annotation_load(annotation_filenamein.c_str());

  // std::cout << "Test load | Result is " << result << std::endl;


  // FILENAME
  // Test save
  // std::string annotation_filenameout = "TESTANNOTATIONOUT";
  // result = rviz_cloud_annotation_load(annotation_filenameout.c_str());

  // std::cout << "Test save | Result is " << result << std::endl;


  // Test set_controlpoint
  result = rviz_cloud_annotation_set_controlpoint(slot, cplist, 0, 7, 10);

  std::cout << "Test set_controlpoint | Result is " << result 
      << " | Setted value is " << cplist[0] << std::endl;
  

  // Test get_labelforpoint
  int activeresult = 31212012;
  activeresult = rviz_cloud_annotation_get_labelforpoint(slot, 0);
  std::cout << "Test get_labelforpoint | Active result is " << activeresult << std::endl;


  // Test get_controlpointlist
  //result = rviz_cloud_annotation_get_controlpointlist(CPData *results);

  //std::cout << "Test get_controlpointlist | Result is " << result << std::endl;


  // Test clear
  result = rviz_cloud_annotation_clear(slot, cplist);

  std::cout << "Test clear | Result is " << result
      << " | Resetted value is " << cplist[0] << std::endl;

  rviz_cloud_annotation_free_data_slot(slot);


  // POINTCLOUD
  // Test clear
  //result = rviz_cloud_annotation_clear(int* results);

  //std::cout << "Test get_controlpointlist | Result is " << result << std::endl;



  return 0;
}
