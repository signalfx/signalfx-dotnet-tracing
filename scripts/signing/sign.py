#!/usr/bin/env python3

import argparse
import enum
import typing


def main():
    parser = argparse.ArgumentParser(
        description='Splunk Signing Self-Service CLI')

    parser.add_argument('paths', metavar='filepath',
                        nargs='+', help='path to a file to be signed')

    types = [x.value for x in list(SignType)]
    parser.add_argument('--type', choices=types, help='signing type')

    args = parser.parse_args()
    sign_type = SignType(args.type)

    sign(args.paths, sign_type)


class SignType(enum.Enum):
    GPG = 'GPG'
    RPM = 'RPM'
    OSX = 'OSX'
    WIN = 'WIN'


def sign(file_paths: typing.Iterable[str], sign_type: SignType) -> None:
    print(file_paths)
    print(sign_type)
    # TODO: implement based on 'def sign_file'


if __name__ == "__main__":
    main()
