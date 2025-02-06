#define EXPORT_API __declspec(dllexport)

extern "C"
{

	EXPORT_API int rviz_cloud_annotation_plugin_test(const int a, int b)
	{
		return a + b;
	}

}