package main

import (
	"io"
	"net/http"
	"os"
	"os/exec"
	"path/filepath"
	"time"
)

// Please don't change the formatting of this line - it's automatically updated by SetAllVersions.cs
const LatestVersion = "0.1.16"

func getAgentVersion() string {
	version, ok := os.LookupEnv("SIGNALFX_DOTNET_TRACING_VERSION")
	if !ok {
		return LatestVersion
	} else {
		return version
	}
}

func getAgentArchiveName() string {
	return "signalfx-dotnet-tracing-" + getAgentVersion() + "-x64.msi"
}

func getAgentDownloadUrl() string {
	return "http://github.com/signalfx/signalfx-dotnet-tracing/releases/download/v" + getAgentVersion() + "/" + getAgentArchiveName()
}

func log(message string) {
	os.Stdout.WriteString("-----> " + message + "\n")
}

func downloadAgentMsi(cacheDir string) (string, error) {
	agentMsiPath := filepath.Join(cacheDir, getAgentArchiveName())
	if _, err := os.Stat(agentMsiPath); os.IsNotExist(err) {
		log("Agent msi file " + agentMsiPath + " does not exist, downloading ...")

		httpClient := &http.Client{
			Timeout: 5 * time.Minute,
		}

		rs, err := httpClient.Get(getAgentDownloadUrl())
		if err != nil {
			return "", err
		}
		defer rs.Body.Close()
		log(rs.Status)

		out, err := os.Create(agentMsiPath)
		if err != nil {
			return "", err
		}
		defer out.Close()

		_, err = io.Copy(out, rs.Body)
		if err != nil {
			return "", err
		}
	}

	return agentMsiPath, nil
}

func unpackAgentMsi(depsDir string, depsIdx string, agentMsiPath string) error {
	libDir := filepath.Join(depsDir, depsIdx, "lib")
	if _, err := os.Stat(libDir); os.IsNotExist(err) {
		err := os.MkdirAll(libDir, 0755)
		if err != nil {
			return err
		}
	}

	log("Unpacking the tracing library from the msi file ...")

	cmd := exec.Command("msiexec", "/a", agentMsiPath, "/qn", "TARGETDIR="+libDir)
	stdout, err := cmd.Output()
	log("msiexec output:\n" + string(stdout))

	if err != nil {
		return err
	}

	return nil
}

func prepareEnvVars(depsIdx string) map[string]string {
	signalFxLibPath := filepath.Join("c:\\Users\\vcap\\deps", depsIdx, "lib", "SignalFx", ".NET Tracing")

	log("Preparing environment variables ...")
	envVars := make(map[string]string)

	// .NET Core profiler
	envVars["CORECLR_ENABLE_PROFILING"] = "1"
	envVars["CORECLR_PROFILER"] = "{B4C89B0F-9908-4F73-9F59-0D77C5A06874}"
	envVars["CORECLR_PROFILER_PATH"] = filepath.Join(signalFxLibPath, "SignalFx.Tracing.ClrProfiler.Native.dll")

	// .NET Framework profiler
	envVars["COR_ENABLE_PROFILING"] = "1"
	envVars["COR_PROFILER"] = "{B4C89B0F-9908-4F73-9F59-0D77C5A06874}"
	envVars["COR_PROFILER_PATH"] = filepath.Join(signalFxLibPath, "SignalFx.Tracing.ClrProfiler.Native.dll")

	// SignalFx tracing config
	envVars["SIGNALFX_INTEGRATIONS"] = filepath.Join(signalFxLibPath, "integrations.json")
	envVars["SIGNALFX_DOTNET_TRACER_HOME"] = signalFxLibPath
	envVars["SIGNALFX_TRACE_LOG_PATH"] = "c:\\Users\\vcap\\logs\\signalfx-dotnet-profiler.log"

	return envVars
}

func writeEnvVars(envVars map[string]string, out *os.File) error {
	for key, value := range envVars {
		if _, err := out.WriteString("set " + key + "=" + value + "\r\n"); err != nil {
			return err
		}
	}
	return nil
}

func writeProfileDScript(depsDir string, depsIdx string, envVars map[string]string) error {
	profileD := filepath.Join(depsDir, depsIdx, "profile.d")
	if _, err := os.Stat(profileD); os.IsNotExist(err) {
		err := os.MkdirAll(profileD, 0755)
		if err != nil {
			return err
		}
	}

	script := filepath.Join(profileD, "configureSfxDotnetTracing.bat")
	log("Writing profile.d start script " + script + " ...")

	out, err := os.Create(script)
	if err != nil {
		return err
	}
	defer out.Close()

	if _, err := out.WriteString("@echo off\r\n"); err != nil {
		return err
	}
	if err := writeEnvVars(envVars, out); err != nil {
		return err
	}

	return nil
}

func writeHwcRunScript(buildDir string, envVars map[string]string) error {
	script := filepath.Join(buildDir, "configureAndRun.bat")
	log("HWC: Writing configureAndRun.bat " + script + " ...")

	scriptOut, err := os.Create(script)
	if err != nil {
		return err
	}
	defer scriptOut.Close()

	if _, err := scriptOut.WriteString("@echo off\r\n"); err != nil {
		return err
	}
	if err := writeEnvVars(envVars, scriptOut); err != nil {
		return err
	}
	if _, err := scriptOut.WriteString(".cloudfoundry\\hwc.exe\r\n"); err != nil {
		return err
	}

	procfile := filepath.Join(buildDir, "Procfile")
	log("HWC: Writing Procfile " + procfile + " ...")

	procfileOut, err := os.Create(procfile)
	if err != nil {
		return err
	}
	defer procfileOut.Close()

	if _, err := procfileOut.WriteString("web: configureAndRun.bat"); err != nil {
		return err
	}

	return nil
}

// Needs to be present because of https://docs.cloudfoundry.org/buildpacks/custom.html#contract
func writeConfigYml(depsDir string, depsIdx string) error {
	configYml := filepath.Join(depsDir, depsIdx, "config.yml")
	log("Writing config.yml " + configYml + " ...")

	out, err := os.Create(configYml)
	if err != nil {
		return err
	}
	defer out.Close()

	if _, err := out.WriteString("---\nname: signalfx-dotnet-tracing\nconfig: {}"); err != nil {
		return err
	}

	return nil
}

func main() {
	if len(os.Args) < 5 {
		log("ERROR: this script must be provided at least 4 args: BUILD_DIR, CACHE_DIR, DEPS_DIR, DEPS_IDX")
		os.Exit(1)
	}

	log("SignalFx Tracing Library for .NET Buildpack")
	var (
		buildDir = os.Args[1]
		cacheDir = os.Args[2]
		depsDir  = os.Args[3]
		depsIdx  = os.Args[4]
	)

	// 1. download agent msi
	agentMsiPath, err := downloadAgentMsi(cacheDir)
	if err != nil {
		log("Cannot download agent MSI file: " + err.Error())
		os.Exit(1)
	}

	// 2. unpack msi
	if err := unpackAgentMsi(depsDir, depsIdx, agentMsiPath); err != nil {
		log("Cannot unpack agent MSI file: " + err.Error())
		os.Exit(1)
	}

	// 3. compute env vars
	envVars := prepareEnvVars(depsIdx)

	// 4. write hwc run script if hwc is used
	if hwc := os.Getenv("SIGNALFX_USE_HWC"); hwc == "true" {
		if err := writeHwcRunScript(buildDir, envVars); err != nil {
			log("Cannot write HWC scripts: " + err.Error())
			os.Exit(1)
		}
	} else {
		// 5. write profile.d script otherwise
		if err := writeProfileDScript(depsDir, depsIdx, envVars); err != nil {
			log("Cannot write profile.d script: " + err.Error())
			os.Exit(1)
		}
	}

	// 6. write empty config.yml
	if err := writeConfigYml(depsDir, depsIdx); err != nil {
		log("Cannot config.yml: " + err.Error())
		os.Exit(1)
	}
}
