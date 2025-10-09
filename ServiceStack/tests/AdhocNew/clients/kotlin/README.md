# Kotlin ServiceStack Client

This is a Kotlin port of the Java ServiceStack client application.

## Project Structure

- `app/src/main/kotlin/org/example/App.kt` - Main application file
- `app/src/main/kotlin/org/example/dtos.kt` - ServiceStack DTOs
- `app/build.gradle.kts` - Gradle build configuration for the app module
- `settings.gradle.kts` - Gradle settings file
- `gradle.properties` - Gradle properties

## Building and Running

### Build the project
```bash
./gradlew build
```

### Run the application
```bash
./gradlew run
```

## Dependencies

- ServiceStack Java Client: 1.1.5
- Gson: 2.11.0
- Kotlin: 2.1.0
- Java Toolchain: 21

## Key Differences from Java Version

The Kotlin version uses:
- Kotlin's `apply` scope function for fluent object initialization
- `val` for immutable variables
- Kotlin's property syntax (`client.bearerToken` instead of `client.setBearerToken()`)
- Top-level `main()` function instead of a class with static main method

