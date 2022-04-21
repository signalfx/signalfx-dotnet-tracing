#pragma once

#include <cstdint>

typedef int32_t (*callback)(int32_t n);
extern "C" int32_t SignalFxCallbackTest(callback fp, int32_t n);