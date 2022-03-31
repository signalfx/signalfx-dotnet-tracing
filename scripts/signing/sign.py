import enum
import typing

DEFAULT_TIMEOUT = 1200
SIGNED_ARTIFACTS_REPO_URL = "https://repo.splunk.com/artifactory/signed-artifacts"
STAGING_URL = "https://repo.splunk.com/artifactory"


class SignType(enum.Enum):
    GPG = "GPG"
    RPM = "RPM"
    OSX = "OSX"
    WIN = "WIN"


def sign(
    file_paths: typing.Iterable[str],
    sign_type: SignType,
    dest: str,
    staging_repo: str,
    staging_user: str,
    staging_pass: str,
    chaperone_tonen: str,
    timeout: int,
) -> None:
    print(locals())
    # TODO: implement based on 'def sign_file'
