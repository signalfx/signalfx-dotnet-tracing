import enum
import hashlib
import os
import random
import time
import requests
import string
import typing

DEFAULT_TIMEOUT = 1200
CHAPERONE_API_URL = "https://chaperone.re.splunkdev.com/api-service"
SIGNED_ARTIFACTS_REPO_URL = "https://repo.splunk.com/artifactory/signed-artifacts"
STAGING_URL = "https://repo.splunk.com/artifactory"


class SignType(enum.Enum):
    GPG = "GPG"
    RPM = "RPM"
    OSX = "OSX"
    WIN = "WIN"


def sign(
    file_paths: typing.Iterable[str],
    dest: str,
    sign_type: SignType,
    staging_repo: str,
    staging_user: str,
    staging_pass: str,
    chaperone_token: str,
    timeout: int,
) -> None:
    # check if files exist
    for path in file_paths:
        assert os.path.isfile(path), f"{path} file not found"
    assert os.path.isdir(dest), f"{path} directory not found"

    for path in file_paths:
        # upload file to artifactory
        subdir = "".join(random.choices(string.ascii_lowercase + string.digits, k=12))
        filename = os.path.basename(path)
        staged_artifact_url = f"{STAGING_URL}/{staging_repo}/{subdir}/{filename}"
        print(f"Uploading {path} to {staged_artifact_url} ...")
        upload_file_to_artifactory(
            path, staged_artifact_url, staging_user, staging_pass
        )
        print(f"Uploaded {path}")

        try:
            # request signing
            print(
                f"Submitting '{sign_type}' signing request for {staged_artifact_url} ..."
            )
            item_key = submit_signing_request(
                staged_artifact_url, sign_type, staging_repo, chaperone_token
            )

            # wait until signed
            print(
                f"Waiting for sign request {item_key} created for {staged_artifact_url} to be completed..."
            )
            artifact_name = f"{filename}.asc" if sign_type == SignType.GPG else filename
            signed_artifact_url = wait_for_signed_artifact(
                item_key,
                artifact_name,
                chaperone_token,
                staging_user,
                staging_pass,
                timeout,
            )

            # download the signed file
            output = os.path.join(dest, artifact_name)
            print(f"Downloading {signed_artifact_url} to {output} ...")
            download_artifactory_file(
                signed_artifact_url, output, staging_user, staging_pass
            )
            print(f"Downloaded {output}")
        finally:
            # remove the uploaded file
            if artifactory_file_exists(staged_artifact_url, staging_user, staging_pass):
                delete_artifactory_file(staged_artifact_url, staging_user, staging_pass)


def upload_file_to_artifactory(src: str, dest: str, user: str, token: str) -> None:
    with open(src, "rb") as fd:
        headers = {"X-Checksum-MD5": get_checksum(src, hashlib.md5())}
        resp = requests.put(dest, auth=(user, token), headers=headers, data=fd)
        assert resp.status_code == 201, f"Upload failed: {resp.reason}\n{resp.text}"


def submit_signing_request(
    src: str, sign_type: SignType, project_key: str, token: str
) -> str:
    headers = {"Accept": "application/json", "Authorization": f"Bearer {token}"}
    data = {
        "artifact_url": src,
        "sign_type": sign_type.value,
        "project_key": project_key,
    }
    resp = requests.post(CHAPERONE_API_URL + "/SIGN/submit", headers=headers, data=data)
    assert (
        resp.status_code == 200
    ), f"Signing request failed: {resp.reason}\n{resp.text}"
    assert (
        "item_key" in resp.json().keys()
    ), f"'item_key' not found in response\n{resp.text}"
    return resp.json().get("item_key")


def wait_for_signed_artifact(
    item_key: str,
    artifact_name: str,
    chaperone_token: str,
    staging_user: str,
    staging_token: str,
    timeout: int,
) -> str:
    start_time = time.time()
    while True:
        assert (time.time() - start_time) < timeout, f"Timed out waiting for {item_key}"

        url = f"{CHAPERONE_API_URL}/{item_key}/check"
        headers = {
            "Accept": "application/json",
            "Authorization": f"Bearer {chaperone_token}",
        }
        resp = requests.get(url, headers=headers)
        assert (
            resp.status_code == 200
        ), f"Chaperone check failed for {item_key}: {resp.reason}\n{resp.text}"
        status = resp.json().get("status", "").lower()
        node = resp.json().get("node", "").lower()
        assert (
            status and node and status != "exception"
        ), f"signing request failed:\n{resp.text}"

        artifact_url = f"{SIGNED_ARTIFACTS_REPO_URL}/{item_key}/{artifact_name}"
        if node == "signed" and artifactory_file_exists(
            artifact_url, staging_user, staging_token
        ):
            break

        time.sleep(10)

    return url


def artifactory_file_exists(url, user, token):
    return requests.head(url, auth=(user, token)).status_code == 200


def download_artifactory_file(url, dest, user, token):
    resp = requests.get(url, auth=(user, token))
    assert resp.status_code == 200, f"Download failed: {resp.reason}\n{resp.text}"
    with open(dest, "wb") as fd:
        fd.write(resp.content)


def delete_artifactory_file(url, user, token):
    resp = requests.delete(url, auth=(user, token))
    assert resp.status_code == 204, f"Delete failed: {resp.reason}\n{resp.text}"


def get_checksum(path: str, hash_obj) -> str:
    with open(path, "rb") as f:
        # Read and update hash string value in blocks of 4K
        for byte_block in iter(lambda: f.read(4096), b""):
            hash_obj.update(byte_block)
        return str(hash_obj.hexdigest()).lower()
