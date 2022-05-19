#include "always_on_profiler_clr_helpers.h"

#include <cstring>

#include "dd_profiler_constants.h"
#include "environment_variables.h"
#include "logger.h"
#include "macros.h"
#include <set>
#include <stack>

#include "../../../shared/src/native-src/pal.h"

namespace trace
{
FunctionInfoNew GetFunctionInfoNew(const ComPtr<IMetaDataImport2>& metadata_import, const mdToken& token)
{
    mdToken parent_token = mdTokenNil;
    mdToken method_spec_token = mdTokenNil;
    WCHAR function_name[kNameMaxSize]{};
    DWORD function_name_len = 0;

    PCCOR_SIGNATURE raw_signature;
    ULONG raw_signature_len;
    BOOL is_generic = false;

    HRESULT hr = E_FAIL;
    switch (const auto token_type = TypeFromToken(token))
    {
        case mdtMemberRef:
            hr = metadata_import->GetMemberRefProps(token, &parent_token, function_name, kNameMaxSize,
                                                    &function_name_len, &raw_signature, &raw_signature_len);
            break;
        case mdtMethodDef:
            hr = metadata_import->GetMemberProps(token, &parent_token, function_name, kNameMaxSize, &function_name_len,
                                                 nullptr, &raw_signature, &raw_signature_len, nullptr, nullptr, nullptr,
                                                 nullptr, nullptr);
            break;
        case mdtMethodSpec:
        {
            hr = metadata_import->GetMethodSpecProps(token, &parent_token, &raw_signature, &raw_signature_len);
            is_generic = true;
            if (FAILED(hr))
            {
                return {};
            }
            const auto generic_info = GetFunctionInfoNew(metadata_import, parent_token);
            std::memcpy(function_name, generic_info.name.c_str(), sizeof(WCHAR) * (generic_info.name.length() + 1));
            function_name_len = DWORD(generic_info.name.length() + 1);
            method_spec_token = token;
        }
        break;
        default:
            Logger::Warn("[trace::GetFunctionInfo] unknown token type: {}", token_type);
            return {};
    }
    if (FAILED(hr) || function_name_len == 0)
    {
        return {};
    }

    // parent_token could be: TypeDef, TypeRef, TypeSpec, ModuleRef, MethodDef
    const auto type_info = GetTypeInfoNew(metadata_import, parent_token);

    if (is_generic)
    {
        return {method_spec_token,
                shared::WSTRING(function_name),
                type_info,
                FunctionMethodSignatureNew(raw_signature, raw_signature_len)};
    }

    return {token, shared::WSTRING(function_name), type_info,
            FunctionMethodSignatureNew(raw_signature, raw_signature_len)};
}

TypeInfoNew GetTypeInfoNew(const ComPtr<IMetaDataImport2>& metadata_import, const mdToken& token)
{
    std::shared_ptr<TypeInfoNew> parentTypeInfo = nullptr;
    mdToken parent_type_token = mdTokenNil;
    WCHAR type_name[kNameMaxSize]{};
    DWORD type_name_len = 0;
    DWORD type_flags;

    HRESULT hr = E_FAIL;

    switch (const auto token_type = TypeFromToken(token))
    {
        case mdtTypeDef:
            hr = metadata_import->GetTypeDefProps(token, type_name, kNameMaxSize, &type_name_len, &type_flags, nullptr);

            metadata_import->GetNestedClassProps(token, &parent_type_token);
            if (parent_type_token != mdTokenNil)
            {
                parentTypeInfo = std::make_shared<TypeInfoNew>(GetTypeInfoNew(metadata_import, parent_type_token));
            }
            break;
        case mdtTypeRef:
            hr = metadata_import->GetTypeRefProps(token, nullptr, type_name, kNameMaxSize, &type_name_len);
            break;
        case mdtTypeSpec:
        {
            PCCOR_SIGNATURE signature{};
            ULONG signature_length{};

            hr = metadata_import->GetTypeSpecFromToken(token, &signature, &signature_length);

            if (FAILED(hr) || signature_length < 3)
            {
                return {};
            }

            if (signature[0] & ELEMENT_TYPE_GENERICINST)
            {
                mdToken type_token;
                CorSigUncompressToken(&signature[2], &type_token);
                const auto baseType = GetTypeInfoNew(metadata_import, type_token);
                return {baseType.id, baseType.name, baseType.parent_type};
            }
        }
        break;
        case mdtModuleRef:
            metadata_import->GetModuleRefProps(token, type_name, kNameMaxSize, &type_name_len);
            break;
        case mdtMemberRef:
        case mdtMethodDef:
            return GetFunctionInfoNew(metadata_import, token).type;
    }
    if (FAILED(hr) || type_name_len == 0)
    {
        return {};
    }

    const auto type_name_string = shared::WSTRING(type_name);
    
    return {token, type_name_string, parentTypeInfo};
}

shared::WSTRING ExtractParameterName(PCCOR_SIGNATURE& pb_cur, const ComPtr<IMetaDataImport2>& metadata_import,
                                     const mdGenericParam* generic_parameters)
{
    pb_cur++;
    ULONG num = 0;
    pb_cur += CorSigUncompressData(pb_cur, &num);
    if (num >= kGenericParamsMaxLen)
    {
        return kUnknown;
    }
    WCHAR param_type_name[kParamNameMaxLen]{};
    ULONG pch_name = 0;
    const auto hr = metadata_import->GetGenericParamProps(generic_parameters[num], nullptr, nullptr, nullptr, nullptr,
                                                          param_type_name, kParamNameMaxLen, &pch_name);
    if (FAILED(hr))
    {
        Logger::Debug("GetGenericParamProps failed. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
        return kUnknown;
    }
    return param_type_name;
}

shared::WSTRING GetSigTypeTokNameNew(PCCOR_SIGNATURE& pb_cur, const ComPtr<IMetaDataImport2>& metadata_import,
                                  mdGenericParam class_params[], mdGenericParam method_params[])
{
    shared::WSTRING token_name = shared::EmptyWStr;
    bool ref_flag = false;
    if (*pb_cur == ELEMENT_TYPE_BYREF)
    {
        pb_cur++;
        ref_flag = true;
    }

    switch (*pb_cur)
    {
        case ELEMENT_TYPE_BOOLEAN:
            token_name = SystemBoolean;
            pb_cur++;
            break;
        case ELEMENT_TYPE_CHAR:
            token_name = SystemChar;
            pb_cur++;
            break;
        case ELEMENT_TYPE_I1:
            token_name = SystemSByte;
            pb_cur++;
            break;
        case ELEMENT_TYPE_U1:
            token_name = SystemByte;
            pb_cur++;
            break;
        case ELEMENT_TYPE_U2:
            token_name = SystemUInt16;
            pb_cur++;
            break;
        case ELEMENT_TYPE_I2:
            token_name = SystemInt16;
            pb_cur++;
            break;
        case ELEMENT_TYPE_I4:
            token_name = SystemInt32;
            pb_cur++;
            break;
        case ELEMENT_TYPE_U4:
            token_name = SystemUInt32;
            pb_cur++;
            break;
        case ELEMENT_TYPE_I8:
            token_name = SystemInt64;
            pb_cur++;
            break;
        case ELEMENT_TYPE_U8:
            token_name = SystemUInt64;
            pb_cur++;
            break;
        case ELEMENT_TYPE_R4:
            token_name = SystemSingle;
            pb_cur++;
            break;
        case ELEMENT_TYPE_R8:
            token_name = SystemDouble;
            pb_cur++;
            break;
        case ELEMENT_TYPE_I:
            token_name = SystemIntPtr;
            pb_cur++;
            break;
        case ELEMENT_TYPE_U:
            token_name = SystemUIntPtr;
            pb_cur++;
            break;
        case ELEMENT_TYPE_STRING:
            token_name = SystemString;
            pb_cur++;
            break;
        case ELEMENT_TYPE_OBJECT:
            token_name = SystemObject;
            pb_cur++;
            break;
        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_VALUETYPE:
        {
            pb_cur++;
            mdToken token;
            pb_cur += CorSigUncompressToken(pb_cur, &token);
            token_name = GetTypeInfoNew(metadata_import, token).name;
            break;
        }
        case ELEMENT_TYPE_SZARRAY:
        {
            pb_cur++;
            token_name = GetSigTypeTokNameNew(pb_cur, metadata_import, class_params, method_params) + WStr("[]");
            break;
        }
        case ELEMENT_TYPE_GENERICINST:
        {
            pb_cur++;
            token_name = GetSigTypeTokNameNew(pb_cur, metadata_import, class_params, method_params);
            token_name += kGenericParamsOpeningBrace;
            ULONG num = 0;
            pb_cur += CorSigUncompressData(pb_cur, &num);
            for (ULONG i = 0; i < num; i++)
            {
                token_name += GetSigTypeTokNameNew(pb_cur, metadata_import, class_params, method_params);
                if (i != num - 1)
                {
                    token_name += kParamsSeparator;
                }
            }
            token_name += kGenericParamsClosingBrace;
            break;
        }
        case ELEMENT_TYPE_MVAR:
        {
            token_name += ExtractParameterName(pb_cur, metadata_import, method_params);
            break;
        }
        case ELEMENT_TYPE_VAR:
        {
            token_name += ExtractParameterName(pb_cur, metadata_import, class_params);
            break;
        }
        default:
            break;
    }

    if (ref_flag)
    {
        token_name += WStr("&");
    }
    return token_name;
}

shared::WSTRING TypeSignatureNew::GetTypeTokName(ComPtr<IMetaDataImport2>& pImport, mdGenericParam class_params[],
                                                 mdGenericParam method_params[]) const
{
    PCCOR_SIGNATURE pbCur = &pbBase[offset];
    return GetSigTypeTokNameNew(pbCur, pImport, class_params, method_params);
}

HRESULT FunctionMethodSignatureNew::TryParse()
{
    PCCOR_SIGNATURE pbCur = pbBase;
    PCCOR_SIGNATURE pbEnd = pbBase + len;
    unsigned char elem_type;

    IfFalseRetFAIL(ParseByte(pbCur, pbEnd, &elem_type));

    if (elem_type & IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
        unsigned gen_param_count;
        IfFalseRetFAIL(ParseNumber(pbCur, pbEnd, &gen_param_count));
    }

    unsigned param_count;
    IfFalseRetFAIL(ParseNumber(pbCur, pbEnd, &param_count));


    IfFalseRetFAIL(ParseRetType(pbCur, pbEnd));

    auto fEncounteredSentinal = false;
    for (unsigned i = 0; i < param_count; i++)
    {
        if (pbCur >= pbEnd) return E_FAIL;

        if (*pbCur == ELEMENT_TYPE_SENTINEL)
        {
            if (fEncounteredSentinal) return E_FAIL;

            fEncounteredSentinal = true;
            pbCur++;
        }

        const PCCOR_SIGNATURE pbParam = pbCur;

        IfFalseRetFAIL(ParseParamOrLocal(pbCur, pbEnd));

        TypeSignatureNew argument{};
        argument.pbBase = pbBase;
        argument.length = (ULONG) (pbCur - pbParam);
        argument.offset = (ULONG) (pbCur - pbBase - argument.length);

        params.push_back(argument);
    }

    return S_OK;
}
} // namespace trace
