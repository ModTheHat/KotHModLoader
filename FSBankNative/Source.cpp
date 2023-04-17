#include <stdio.h>
#include <iostream>
#include <FSB/fsbank.h>
#include <FSB/fsbank_errors.h>
#include <string>
#include <array>

using namespace std;
using std::array;

extern "C" _declspec(dllexport) void Create();

void Create()
{
	FSBANK_RESULT result = FSBank_Init(FSBANK_FSBVERSION_FSB5, FSBANK_INIT_NORMAL, 2, "cache");

	FSBANK_SUBSOUND subsound = { };
	string path = "D:/KotHModLoader/KotHModLoaderGUI/bin/Debug/Mods/New folder/KotH_UI_LandingScreen_PressStart_v2-01.ogg";
	const char* const msg = path.c_str();
	subsound.fileNames = &msg;
	cout << *subsound.fileNames;
	subsound.overrideFlags = FSBANK_BUILD_DISABLESYNCPOINTS;
	subsound.desiredSampleRate = 0;
	subsound.numFiles = 1;
	const FSBANK_SUBSOUND* newSubsound = &subsound;
	const char* output = "test.fsb5";

	result = FSBank_Build(newSubsound, 1, FSBANK_FORMAT_VORBIS, FSBANK_BUILD_DEFAULT | FSBANK_BUILD_DONTLOOP | FSBANK_BUILD_WRITEPEAKVOLUME, 82, NULL, output);
	cout << FSBANK_RESULT(result) << endl;

	getchar();

}
