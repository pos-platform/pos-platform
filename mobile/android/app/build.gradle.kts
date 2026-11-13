import java.io.FileInputStream
import java.util.Properties

plugins {
    id("com.android.application")
    // The Flutter Gradle Plugin must be applied after the Android and Kotlin Gradle plugins.
    id("dev.flutter.flutter-gradle-plugin")
}

// Firebase (push): google-services.json hali qo'shilmagan bo'lsa plagin qo'llanmaydi — build buzilmaydi,
// ilovada push jimgina o'chadi. Batafsil: mobile/docs/PUSH.md.
if (file("google-services.json").exists()) {
    apply(plugin = "com.google.gms.google-services")
}

// Release imzo kaliti: android/key.properties (repoga KIRMAYDI, .gitignore'da).
// Namuna: android/key.properties.example. Batafsil: mobile/docs/RELEASE.md.
val keystorePropertiesFile = rootProject.file("key.properties")
val keystoreProperties = Properties()
val hasReleaseKeystore = keystorePropertiesFile.exists()
if (hasReleaseKeystore) {
    FileInputStream(keystorePropertiesFile).use { keystoreProperties.load(it) }
}

// Do'kon (white-label) ilovasi: android/store.properties (tool/store.ps1 yozadi, repoga KIRMAYDI).
// Bo'lmasa — standart "PosCloud Market" (uz.poscloud.poscloud_market). Batafsil: mobile/docs/WHITE_LABEL.md.
val storePropertiesFile = rootProject.file("store.properties")
val storeProperties = Properties()
if (storePropertiesFile.exists()) {
    FileInputStream(storePropertiesFile).use { storeProperties.load(it) }
}
val storeApplicationId: String = storeProperties.getProperty("applicationId")?.trim()?.takeIf { it.isNotEmpty() }
    ?: "uz.poscloud.poscloud_market"
val storeAppLabel: String = storeProperties.getProperty("appLabel")?.trim()?.takeIf { it.isNotEmpty() }
    ?: "Xo‘ja Market"

android {
    // namespace (Kotlin paketi) do'kon ilovalarida ham o'zgarmaydi — faqat applicationId o'zgaradi.
    namespace = "uz.poscloud.poscloud_market"
    // flutter_local_notifications va androidx.core 1.19 kamida 37 talab qiladi (minSdk'ga ta'sir qilmaydi).
    compileSdk = maxOf(flutter.compileSdkVersion, 37)
    ndkVersion = flutter.ndkVersion

    compileOptions {
        // flutter_local_notifications talabi (java.time eski Android'larda).
        isCoreLibraryDesugaringEnabled = true
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    defaultConfig {
        // DIQQAT: Play'da nashr qilingandan keyin applicationId'ni O'ZGARTIRIB BO'LMAYDI.
        // Do'kon ilovasida — store.properties dagi applicationId.
        applicationId = storeApplicationId
        // Ilova nomi (AndroidManifest: android:label="${appLabel}").
        manifestPlaceholders["appLabel"] = storeAppLabel
        // You can update the following values to match your application needs.
        // For more information, see: https://flutter.dev/to/review-gradle-config.
        // sentry_flutter kamida 26 (Android 8.0) talab qiladi.
        minSdk = maxOf(flutter.minSdkVersion, 26)
        targetSdk = flutter.targetSdkVersion
        // versionCode/versionName pubspec.yaml dagi `version: X.Y.Z+N` dan olinadi.
        versionCode = flutter.versionCode
        versionName = flutter.versionName
    }

    signingConfigs {
        if (hasReleaseKeystore) {
            create("release") {
                keyAlias = keystoreProperties.getProperty("keyAlias")
                keyPassword = keystoreProperties.getProperty("keyPassword")
                // Nisbiy yo'l android/app papkasiga nisbatan hisoblanadi.
                storeFile = keystoreProperties.getProperty("storeFile")?.let { file(it) }
                storePassword = keystoreProperties.getProperty("storePassword")
            }
        }
    }

    buildTypes {
        release {
            // key.properties bo'lsa — haqiqiy upload kalit bilan imzolanadi.
            // Bo'lmasa — debug kalit bilan (CI/dev yig'ishlar va `flutter run --release` ishlashi uchun).
            // DIQQAT: debug imzoli AAB'ni Play Console qabul qilmaydi.
            signingConfig = if (hasReleaseKeystore) {
                signingConfigs.getByName("release")
            } else {
                signingConfigs.getByName("debug")
            }
        }
    }
}

kotlin {
    compilerOptions {
        jvmTarget = org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17
    }
}

flutter {
    source = "../.."
}

dependencies {
    coreLibraryDesugaring("com.android.tools:desugar_jdk_libs:2.1.4")
}
