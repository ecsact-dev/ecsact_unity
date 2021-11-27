workspace(name = "ecs_idl_unity")

load("@bazel_tools//tools/build_defs/repo:http.bzl", "http_archive")
load("@bazel_tools//tools/build_defs/repo:git.bzl", "git_repository")

local_repository(
    name = "ecs_idl",
    path = "C:/projects/seaube/ecs-idl",
)

http_archive(
    name = "boost",
    strip_prefix = "boost-95b7ebcdf8d837452efedaa342858360c7fb58e4",
    urls = ["https://github.com/bazelboost/boost/archive/95b7ebcdf8d837452efedaa342858360c7fb58e4.zip"],
    sha256 = "13d59f4265e3d29d63d17ad6c439d2f8663baea6b8f02961d638348085aa600f",
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
