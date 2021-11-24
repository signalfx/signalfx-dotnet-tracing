// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full
// license information.

#include "dllmain.h"
#include "class_factory.h"

const IID IID_IUnknown = {0x00000000, 0x0000, 0x0000, {0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46}};

const IID IID_IClassFactory = {0x00000001, 0x0000, 0x0000, {0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46}};

HINSTANCE DllHandle;

extern "C"
{
    BOOL STDMETHODCALLTYPE DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
    {
        DllHandle = hModule;
        return TRUE;
    }

    HRESULT STDMETHODCALLTYPE DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID* ppv) {
        // {B4C89B0F-9908-4F73-9F59-0D77C5A06874}
        const GUID CLSID_CorProfiler = {0xb4c89b0f, 0x9908, 0x4f73, {0x9f, 0x59, 0xd, 0x77, 0xc5, 0xa0, 0x68, 0x74}};

        // {0F171A24-3497-4B05-AE6D-B6B313FBF83B}
        const GUID CLSID_New_CorProfiler = {0x0f171a24, 0x3497, 0x4b05, {0xae, 0x6d, 0xb6, 0xb3, 0x13, 0xfb, 0xf8, 0x3b}};

        if (ppv == NULL || (rclsid != CLSID_CorProfiler && rclsid != CLSID_New_CorProfiler))
        {
            return E_FAIL;
        }

        auto factory = new ClassFactory;

        if (factory == NULL)
        {
            return E_FAIL;
        }

        return factory->QueryInterface(riid, ppv);
    }

    HRESULT STDMETHODCALLTYPE DllCanUnloadNow()
    {
        return S_OK;
    }
}
