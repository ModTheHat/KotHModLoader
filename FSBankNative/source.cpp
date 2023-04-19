#include <stdio.h>
#include <iostream>
#include <fsbank.h>
#include <fsbank_errors.h>
#include <string>
#include <array>

using namespace std;
using std::array;

extern "C" _declspec(dllexport) int Create(const char* path);

int Create(const char* path)
{
	FSBANK_RESULT result = FSBank_Init(FSBANK_FSBVERSION_FSB5, FSBANK_INIT_NORMAL, 2, "cache");

	FSBANK_SUBSOUND subsound = { };
	//string path = "D:/KotHModLoader - Copy (2)/KotHModLoaderGUI/bin/Debug/Mods/New folder/KotH_VO_Cole_Soc_Happy_Short_v2-01-11.ogg";
	string path2 = "D:/file_example_WAV_1MG.wav";
	const char* const msg = path;
	subsound.fileNames = &msg;
	cout << *subsound.fileNames;
	subsound.overrideFlags = FSBANK_BUILD_DISABLESYNCPOINTS;
	subsound.desiredSampleRate = 0;
	subsound.numFiles = 1;
	const FSBANK_SUBSOUND* newSubsound = &subsound;
	const char* output = ((string)path + ".fsb").c_str();
	output = "temp.fsb";

	result = FSBank_Build(newSubsound, 1, FSBANK_FORMAT_VORBIS, FSBANK_BUILD_DEFAULT | FSBANK_BUILD_DONTLOOP | FSBANK_BUILD_WRITEPEAKVOLUME, 82, NULL, output);
	cout << FSBANK_RESULT(result) << endl;
	cout << FSBank_ErrorString(result) << endl;
	return result;
	getchar();
}