workspace(name = "ecsact_unity")

load("@bazel_tools//tools/build_defs/repo:http.bzl", "http_archive")
load("@bazel_tools//tools/build_defs/repo:git.bzl", "git_repository")

git_repository(
    name = "ecsact_rtb",
    commit = "4d4856d697300a1e3c8b2e49b25acb72e9328b9d",
    remote = "git@github.com:seaube/ecsact-rtb.git",
    # shallow_since = "1657554031 -0700",
)

load("@ecsact_rtb//:repositories.bzl", "ecsact_rtb_repositories")

ecsact_rtb_repositories()

load("@ecsact_rtb//:workspace.bzl", "ecsact_rtb_workspace")

ecsact_rtb_workspace()

http_archive(
    name = "bzlws",
    sha256 = "9bc9d6bf1d885992d58a4ad9dc7476a8cd48d672b497707b0ae2c0899c6d369b",
    strip_prefix = "bzlws-344801b9b3105bd13e4b51ec9776f04bd5e01972",
    url = "https://github.com/zaucy/bzlws/archive/344801b9b3105bd13e4b51ec9776f04bd5e01972.zip",
)

load("@bzlws//:repo.bzl", "bzlws_deps")

bzlws_deps()

http_archive(
    name = "rules_7zip",
    sha256 = "29ba984e2a7d48540faa839efaf09be4b880d211a93575e7ac87abffc12dbdea",
    strip_prefix = "rules_7zip-25d3b858a37580dbc1f1ced002e210be15012e2f",
    urls = ["https://github.com/zaucy/rules_7zip/archive/25d3b858a37580dbc1f1ced002e210be15012e2f.zip"],
)

load("@rules_7zip//:setup.bzl", "setup_7zip")

setup_7zip()

_nlohmann_json_build_file = """
load("@rules_cc//cc:defs.bzl", "cc_library")

cc_library(
    name = "json",
    visibility = ["//visibility:public"],
    includes = ["include"],
    hdrs = glob(["include/**/*.hpp"]),
    strip_include_prefix = "include",
)
"""

http_archive(
    name = "nlohmann_json",
    build_file_content = _nlohmann_json_build_file,
    sha256 = "62c585468054e2d8e7c2759c0d990fd339d13be988577699366fe195162d16cb",
    url = "https://github.com/nlohmann/json/releases/download/v3.10.4/include.zip",
)

http_archive(
    name = "build_bazel_rules_nodejs",
    sha256 = "ddb78717b802f8dd5d4c01c340ecdc007c8ced5c1df7db421d0df3d642ea0580",
    urls = ["https://github.com/bazelbuild/rules_nodejs/releases/download/4.6.0/rules_nodejs-4.6.0.tar.gz"],
)

load("@build_bazel_rules_nodejs//:index.bzl", "node_repositories")

node_repositories()

http_archive(
    name = "aspect_bazel_lib",
    sha256 = "534c9c61b72c257c95302d544984fd8ee63953c233292c5b6952ca5b33cd225e",
    strip_prefix = "bazel-lib-0.4.2",
    url = "https://github.com/aspect-build/bazel-lib/archive/v0.4.2.tar.gz",
)

load("@aspect_bazel_lib//lib:repositories.bzl", "aspect_bazel_lib_dependencies")

aspect_bazel_lib_dependencies()

http_archive(
    name = "rules_pkg",
    sha256 = "8a298e832762eda1830597d64fe7db58178aa84cd5926d76d5b744d6558941c2",
    urls = [
        "https://mirror.bazel.build/github.com/bazelbuild/rules_pkg/releases/download/0.7.0/rules_pkg-0.7.0.tar.gz",
        "https://github.com/bazelbuild/rules_pkg/releases/download/0.7.0/rules_pkg-0.7.0.tar.gz",
    ],
)

load("@rules_pkg//:deps.bzl", "rules_pkg_dependencies")

rules_pkg_dependencies()
