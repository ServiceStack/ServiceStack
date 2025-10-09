// Package.swift
// swift-tools-version:6.0.1
import PackageDescription

let package = Package(
    name: "ServiceStackExample",
    platforms: [
        .macOS(.v10_15),
        .iOS(.v13),
        .tvOS(.v13),
        .watchOS(.v6)
    ],
    dependencies: [
        .package(url: "https://github.com/ServiceStack/ServiceStack.Swift.git", from: "6.0.5")
    ],
    targets: [
        .executableTarget(
            name: "ServiceStackExample",
            dependencies: [
                .product(name: "ServiceStack", package: "ServiceStack.Swift")
            ],
            path: ".",
            exclude: ["README.md", "FIXES.md"]
        )
    ]
)