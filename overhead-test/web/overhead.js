
const MARKETING_NAMES = {
    'none': 'Not instrumented',
    'signalfx-dotnet': 'SignalFx Instrumentation for .NET',
    'signalfx-dotnet-with-profiling-10s': 'SignalFx Instrumentation for .NET with AlwaysOn CPU Profiling',
    'signalfx-dotnet-with-profiling-1s': 'SignalFx Instrumentation for .NET with AlwaysOn CPU Profiling - high frequency sampling',
    'signalfx-dotnet-with-mem-profiling': 'SignalFx Instrumentation for .NET with AlwaysOn Memory Profiling',
    'signalfx-dotnet-with-cpu-and-mem-profiling' : 'SignalFx Instrumentation for .NET with AlwaysOn CPU and Memory Profiling'
}

async function startOverhead() {
    console.log('overhead started');
    document.getElementById('test-run')
        .addEventListener("change", testRunChosen);
    getRuns()
        .then(runNames => {
            console.log(runNames);
            populateRunsDropDown(runNames);
            document.getElementById('test-run').value = runNames[0];
            testRunChosen(runNames[0]);
        });
}

async function testRunChosen() {
    const value = document.getElementById('test-run').value;
    console.log(`selection changed ${value}`);
    const config = await getConfig(value);
    const results = await getResults(value, config)
    addOverview(config);
    addCharts(results);
}

function addOverview(config) {
    const overview = document.getElementById('overview');
    overview.innerHTML = '';
    addMainOverview(overview, config);
    addAgents(overview, config);
}

function addMainOverview(overview, config) {
    const title = document.createElement('h4');
    if(!config.name){
        title.innerText = '<<unavailable>>';
        overview.append(title);
        return;
    }
    title.innerText = config.name;
    const desc = document.createElement('p');
    desc.innerText = config.description;
    const list = document.createElement('ul');

    addListItem(list, `<b>concurrent connections</b>: ${config.concurrentConnections}`);
    addListItem(list, `<b>max rate</b>: ${config.maxRequestRate} rps`);
    addListItem(list, `<b>script iterations</b>: ${config.totalIterations}`);

    overview.append(title, desc, list);
}

function addAgents(overview, config) {
    if(!config.agents) return;
    config.agents.forEach(agent => {
        const card = document.createElement('div');
        card.classList.add('card', 'my-2');
        card.style = 'width: 25rem;';
        const body = document.createElement('div');
        body.classList.add('card-body');
        const title = document.createElement('h5')
        title.classList.add('card-title');
        title.innerText = agent.name;
        const subtitle = document.createElement('h6');
        subtitle.classList.add('card-subtitle', 'mb-2', 'text-muted');
        subtitle.innerText = agent.description;
        body.append(title);
        body.append(subtitle);
        card.append(body);
        overview.append(card);
        if(agent.additionalEnvVars.length > 0){
            const p = document.createElement('p');
            p.innerText = 'Additional environment variables set:';
            body.append(p)
            const args = document.createElement('ul');
            agent.additionalEnvVars.forEach(arg => {
                addListItem(args, `${arg.name}=${arg.value}`, ['font-monospace', 'envvar']);
            });
            body.append(args);
        }
    });
}

function addListItem(list, text, classes = []) {
    const li = document.createElement('li')
    classes.forEach(c => li.classList.add(c));
    li.innerHTML = text;
    list.append(li);
}

function populateRunsDropDown(runNames) {
    const sel = document.getElementById('test-run');
    runNames.forEach(name => {
        const option = document.createElement("option");
        option.text = name;
        option.value = name;
        sel.add(option);
    });
}

function addCharts(aggregated) {
    makeChart(aggregated, 'averageCpuUsage', "% CPU load");
    makeChart(aggregated, 'averageWorkingSet', "Megabytes");
    makeChart(aggregated, 'maxHeapUsed', "Megabytes");
    makeChart(aggregated, 'minHeapUsed', "Megabytes");
    makeChart(aggregated, 'totalAllocatedMB', "Megabytes");
    makeChart(aggregated, 'averageTimeSpentInGc', "% Time");
    makeChart(aggregated, 'iterationAvg', "Milliseconds");
    makeChart(aggregated, 'iterationP95', "Milliseconds");
    makeChart(aggregated, 'requestAvg', "Milliseconds");
    makeChart(aggregated, 'requestP95', "Milliseconds");
    makeChart(aggregated, 'maxThreadPoolThreadCount', "Count");
}

function makeMarketingNames(agents) {
    return agents.map(a => MARKETING_NAMES[a] || a);
}

function makeChart(aggregated, resultType, axisTitle, scaleFunction = x => x) {
    const agents = aggregated['agents'];
    const marketingNames = makeMarketingNames(agents);
    const initialResults = agents.map(agent => aggregated['results'][resultType][agent]);
    const results = initialResults.map(scaleFunction);
    new Chartist.Bar(`#${resultType}-chart`, {
            labels: marketingNames,
            series: [results]
        },
        {
            seriesBarDistance: 10,
            axisX: {
                offset: 60
            },
            axisY: {
                offset: 60,
                scaleMinSpace: 20
            },
            plugins: [
                Chartist.plugins.ctBarLabels({
                    labelClass: 'ct-bar-label',
                    labelInterpolationFnc: function (text) {
                        return text.toFixed(2);
                    }
                }),
                Chartist.plugins.ctAxisTitle({
                    axisY: {
                        axisTitle: axisTitle,
                        axisClass: "ct-axis-title",
                        offset: {
                            x: 0,
                            y: 15
                        },
                        flipTitle: true
                    }
                })
            ]
        },
    );
}
