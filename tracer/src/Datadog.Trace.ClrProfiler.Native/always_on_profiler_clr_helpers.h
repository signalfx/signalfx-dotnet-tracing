#pragma once

#include <corhlpr.h>
#include <corprof.h>
#include <functional>
#include <utility>

#include "integration.h"

#include "./clr_helpers.h"

#include <set>

// this file contains optimized, specified version of code from clr_helpers.h

namespace always_on_profiler
{
constexpr auto kParamNameMaxLen = 260;
constexpr auto kGenericParamsMaxLen = 20;
constexpr auto kUnknown = WStr("Unknown");
constexpr auto kParamsSeparator = WStr(", ");
constexpr auto kGenericParamsOpeningBrace = WStr("[");
constexpr auto kGenericParamsClosingBrace = WStr("]");
constexpr auto kFunctionParamsOpeningBrace = WStr("(");
constexpr auto kFunctionParamsClosingBrace = WStr(")");
constexpr auto name_separator = WStr(".");

struct TypeInfo
{
    const mdToken id;
    const shared::WSTRING name;

    TypeInfo() :
        id(0),
        name(shared::EmptyWStr)
    {
    }
    TypeInfo(const mdToken id, const shared::WSTRING name) :
        id(id),
        name(name)
    {
    }
};

struct TypeSignature
{
    ULONG offset;
    ULONG length;
    PCCOR_SIGNATURE pbBase;

    shared::WSTRING GetTypeTokName(ComPtr<IMetaDataImport2>& pImport, mdGenericParam class_params[],
                                   mdGenericParam method_params[]) const;
};

struct FunctionMethodSignature
{
private:
    PCCOR_SIGNATURE pbBase;
    unsigned len;
    std::vector<TypeSignature> params;

public:
    FunctionMethodSignature() : pbBase(nullptr), len(0)
    {
    }
    FunctionMethodSignature(PCCOR_SIGNATURE pb, unsigned cbBuffer)
    {
        pbBase = pb;
        len = cbBuffer;
    };
    const std::vector<TypeSignature>& GetMethodArguments() const
    {
        return params;
    }
    HRESULT TryParse();
};

struct FunctionInfo
{
    const mdToken id;
    const shared::WSTRING name;
    const TypeInfo type;
    FunctionMethodSignature method_signature;

    FunctionInfo() : id(0), name(shared::EmptyWStr), type({}), method_signature({})
    {
    }

    FunctionInfo(mdToken id, shared::WSTRING name, TypeInfo type,
                 FunctionMethodSignature method_signature) :
        id(id),
        name(name),
        type(type),
        method_signature(method_signature)
    {
    }

    bool IsValid() const
    {
        return id != 0;
    }
};

FunctionInfo GetFunctionInfo(const ComPtr<IMetaDataImport2>& metadata_import, const mdToken& token);

TypeInfo GetTypeInfo(const ComPtr<IMetaDataImport2>& metadata_import, const mdToken& token);

} // namespace always_on_profiler
