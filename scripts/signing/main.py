#!/usr/bin/env python3

import argparse
from sign import sign, SignType

DEFAULT_TIMEOUT = 1200
SIGNED_ARTIFACTS_REPO_URL = "https://repo.splunk.com/artifactory/signed-artifacts"
STAGING_URL = "https://repo.splunk.com/artifactory"


def main():
    parser = argparse.ArgumentParser(description="Splunk Signing Self-Service CLI")

    parser.add_argument(
        "paths", metavar="filepath", nargs="+", help="path to a file to be signed"
    )

    parser.add_argument("--dest", required=True, help="output directory")

    types = [x.value for x in list(SignType)]
    parser.add_argument("--type", choices=types, help="signing type")

    parser.add_argument(
        "--staging-repo", required=True, help="signing staging repository"
    )
    parser.add_argument(
        "--staging-user", required=True, help="signing staging username"
    )
    parser.add_argument(
        "--staging-pass", required=True, help="signing staging password"
    )
    parser.add_argument(
        "--chaperone-token", required=True, help="signing Chaperone token"
    )

    parser.add_argument(
        "--timeout",
        type=int,
        default=DEFAULT_TIMEOUT,
        help=f"signing request timeout in seconds (defaults: {DEFAULT_TIMEOUT})",
    )

    args = parser.parse_args()
    sign_type = SignType(args.type)

    sign(
        args.paths,
        args.dest,
        sign_type,
        args.staging_repo,
        args.staging_user,
        args.staging_pass,
        args.chaperone_token,
        args.timeout,
    )


if __name__ == "__main__":
    main()
