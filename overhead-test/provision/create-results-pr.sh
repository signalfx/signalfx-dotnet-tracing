#!/bin/bash

# Creates a PR into main branch from the results

MYDIR=$(dirname $0)
RESULTS=${MYDIR}/../web/results
REV=$(ls "${RESULTS}")
NEW_BRANCH="results_${REV}"

set -e

echo ">>> Setting GnuPG configuration ..."
mkdir -p ~/.gnupg
chmod 700 ~/.gnupg
cat > ~/.gnupg/gpg.conf <<EOF
no-tty
pinentry-mode loopback
EOF

echo ">>> Importing secret key ..."
gpg --batch --allow-secret-key-import --import ${GITHUB_BOT_GPG_KEY}

echo ">>> Setting up git config options"
GPG_KEY_ID=$(gpg2 -K --keyid-format SHORT | grep '^ ' | tr -d ' ')
git config --global user.name srv-gh-o11y-gdi-dotnet-test
git config --global user.email ssg-srv-gh-o11y-gdi-dotnet-test@splunk.com
git config --global gpg.program gpg
git config --global user.signingKey ${GPG_KEY_ID}

git clone https://srv-gh-o11y-gdi-dotnet-test:"${GITHUB_TOKEN}"@github.com/signalfx/signalfx-dotnet-tracing.git github-clone
cd github-clone
git checkout main
git checkout -b ${NEW_BRANCH}
cd ..

echo "Setting up a new pull request for results data: ${REV} results"
MSG="Add test results: ${REV}"
RESULTS_TARGET_DIR="overhead-test/web/results/"

mkdir -p $RESULTS_TARGET_DIR

rsync -avv --progress "${RESULTS}/${REV}" github-clone/${RESULTS_TARGET_DIR}
cd github-clone
echo "Results list: " && ls -l ${RESULTS_TARGET_DIR}
ls -1 ${RESULTS_TARGET_DIR} | grep -v README | grep -v index.txt > ${RESULTS_TARGET_DIR}/index.txt
echo "Adding new files to changelist"
git add ${RESULTS_TARGET_DIR}/index.txt
git add ${RESULTS_TARGET_DIR}/${REV}/*
echo "Committing changes..."
git commit -S -am "[automated] $MSG"
git show HEAD
# TODO: uncomment when verified
#echo "Pushing results to remote branch ${NEW_BRANCH}"
#git push https://srv-gh-o11y-gdi-dotnet-test:"${GITHUB_TOKEN}"@github.com/signalfx/signalfx-dotnet-tracing.git ${NEW_BRANCH}
#
#echo "Running PR create command:"
#gh pr create \
#  --title "$MSG" \
#  --body "$MSG" \
#  --label automated \
#  --base main \
#  --head "$NEW_BRANCH"
