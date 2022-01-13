workspace(name = "ecs_idl_unity")

load("@bazel_tools//tools/build_defs/repo:http.bzl", "http_archive")
load("@bazel_tools//tools/build_defs/repo:git.bzl", "git_repository")

git_repository(
    name = "ecs_idl",
    remote = "git@github.com:seaube/ecs-idl.git",
    commit = "e695dbbbadea47ebf3a22c806b7c282cd617206f",
    shallow_since = "1641923718 -0800",
)

http_archive(
    name = "boost",
    strip_prefix = "boost-563e8e0de4eac4b48a02d296581dc2454127608e",
    urls = ["https://github.com/bazelboost/boost/archive/563e8e0de4eac4b48a02d296581dc2454127608e.zip"],
    sha256 = "c41441a6e9f8038ad626e9f937ddc3675ab896b6c3512eefc6840037b3816d03",
)

load("@boost//:index.bzl", "boost_http_archives")
boost_http_archives()

http_archive(
    name = "bzlws",
    strip_prefix = "bzlws-f929e5380f441f50a77776d34a7df8cacdbdf986",
    url = "https://github.com/zaucy/bzlws/archive/f929e5380f441f50a77776d34a7df8cacdbdf986.zip",
    sha256 = "5bebb821b158b11d81dd25cf031b5b26bae97dbb02025df7d0e41a262b3a030b",
)

load("@bzlws//:repo.bzl", "bzlws_deps")
bzlws_deps()

http_archive(
    name = "rules_7zip",
    strip_prefix = "rules_7zip-25d3b858a37580dbc1f1ced002e210be15012e2f",
    urls = ["https://github.com/zaucy/rules_7zip/archive/25d3b858a37580dbc1f1ced002e210be15012e2f.zip"],
    sha256 = "29ba984e2a7d48540faa839efaf09be4b880d211a93575e7ac87abffc12dbdea",
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
    url = "https://github.com/nlohmann/json/releases/download/v3.10.4/include.zip",
    sha256 = "62c585468054e2d8e7c2759c0d990fd339d13be988577699366fe195162d16cb",
    build_file_content = _nlohmann_json_build_file,
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
