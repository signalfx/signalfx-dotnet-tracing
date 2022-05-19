#pragma once

#include <corhlpr.h>
#include <corprof.h>
#include <functional>
#include <utility>

#include "integration.h"

#include "./clr_helpers.h"

#include <set>

namespace trace
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

struct TypeInfoNew
{
    const mdToken id;
    const shared::WSTRING name;

    TypeInfoNew() :
        id(0),
        name(shared::EmptyWStr)
    {
    }
    TypeInfoNew(const mdToken id, const shared::WSTRING name) :
        id(id),
        name(name)
    {
    }
};

struct TypeSignatureNew
{
    ULONG offset;
    ULONG length;
    PCCOR_SIGNATURE pbBase;

    shared::WSTRING GetTypeTokName(ComPtr<IMetaDataImport2>& pImport, mdGenericParam class_params[],
                                   mdGenericParam method_params[]) const;
};

struct FunctionMethodSignatureNew
{
private:
    PCCOR_SIGNATURE pbBase;
    unsigned len;
    // ULONG numberOfTypeArguments = 0; verify if it can be removed
    // ULONG numberOfArguments = 0;
    // TypeSignatureNew returnValue{};
    std::vector<TypeSignatureNew> params;

public:
    FunctionMethodSignatureNew() : pbBase(nullptr), len(0)
    {
    }
    FunctionMethodSignatureNew(PCCOR_SIGNATURE pb, unsigned cbBuffer)
    {
        pbBase = pb;
        len = cbBuffer;
    };
    const std::vector<TypeSignatureNew>& GetMethodArguments() const
    {
        return params;
    }
    HRESULT TryParse();
};

struct FunctionInfoNew
{
    const mdToken id;
    const shared::WSTRING name;
    const TypeInfoNew type;
    FunctionMethodSignatureNew method_signature;

    FunctionInfoNew() : id(0), name(shared::EmptyWStr), type({}), method_signature({})
    {
    }

    FunctionInfoNew(mdToken id, shared::WSTRING name, TypeInfoNew type,
                 FunctionMethodSignatureNew method_signature) :
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

FunctionInfoNew GetFunctionInfoNew(const ComPtr<IMetaDataImport2>& metadata_import, const mdToken& token);

TypeInfoNew GetTypeInfoNew(const ComPtr<IMetaDataImport2>& metadata_import, const mdToken& token);

mdAssemblyRef FindAssemblyRef(const ComPtr<IMetaDataAssemblyImport>& assembly_import,
                              const shared::WSTRING& assembly_name, const Version& version);

HRESULT GetCorLibAssemblyRef(const ComPtr<IMetaDataAssemblyEmit>& assembly_emit, AssemblyProperty& corAssemblyProperty,
                             mdAssemblyRef* corlib_ref);

bool FindTypeDefByName(const shared::WSTRING instrumentationTargetMethodTypeName, const shared::WSTRING assemblyName,
                       const ComPtr<IMetaDataImport2>& metadata_import, mdTypeDef& typeDef);
} // namespace trace
