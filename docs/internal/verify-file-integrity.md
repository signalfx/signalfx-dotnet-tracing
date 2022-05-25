# Verify file integrity

## Checksums

The `checksums.txt` lists SHA-256 hash of all published artifacts.
You can use `checksums.txt` to verify the integrity of all published artifacts
using `shasum`.

```bash
shasum -a 256 -c checksums.txt
```

`checksums.txt.asc` is a PGP signature file `checksums.txt`.
You can use [SplunkPGPKey.pub](https://docs.splunk.com/images/6/6b/SplunkPGPKey.pub)
and `checksums.txt.asc` to verify the integrity of `checksums.txt`.

```bash
curl -O https://docs.splunk.com/images/6/6b/SplunkPGPKey.pub
gpg --import SplunkPGPKey.pub
gpg --verify checksums.txt.asc checksums.txt
```

## DEB package

You can use [SplunkPGPKey.pub](https://docs.splunk.com/images/6/6b/SplunkPGPKey.pub)
to verify the integrity of the DEB packages using `dpkg-sig`.

```bash
curl -O https://docs.splunk.com/images/6/6b/SplunkPGPKey.pub
gpg --import SplunkPGPKey.pub
dpkg-sig --verify signalfx-dotnet-tracing-*.deb
```

## RPM package

You can use [SplunkPGPKey.pub](https://docs.splunk.com/images/6/6b/SplunkPGPKey.pub)
to verify the integrity of the RPM packages.

```bash
curl -O https://docs.splunk.com/images/6/6b/SplunkPGPKey.pub
rpm --import SplunkPGPKey.pub
rpm -K signalfx-dotnet-tracing-*.rpm
```
