/**
 * The parsed format is like this:
 * {
 *   agents: [a1, a2, a3],
 *   runs: [
 *       {
 *           timestamp: 123456,
 *           a1: {
 *               f1: v1,
 *               f2: v2,
 *               f3: v3
 *           },
 *           a2: {
 *               f1: v1,
 *               f2: v2,
 *               ...
 *           }
 *       }
 *   ]
 * }
 */
function parseCsv(body, config) {
    const runs = parseRuns(body);
    const agents = config.agents.map(config => config.name);
    console.log(agents);
    return {
        agents: agents,
        runs: runs
    }
}

function parseRuns(body) {
    const lines = body.trim().split("\n");
    const fieldNames = lines.shift().split(",");

    return lines.map(line => {
        const fields = line.split(",");
        const timestamp = fields.shift();
        const fieldTuples = fields.map((elem, i) => {
            const agent = fieldNames[i + 1].replace(/:.*/, '');
            const fieldName = fieldNames[i + 1].replace(/.*:/, '');
            return [agent, fieldName, elem];
        });
        const obj = fieldTuples.reduce((acc, tuple) => {
            const agent = tuple[0];
            const obj = acc[agent] || {};
            const fieldName = tuple[1];
            const fieldValue = tuple[2];
            obj[fieldName] = Number(fieldValue);
            acc[agent] = obj;
            return acc;
        }, {});
        obj['timestamp'] = new Date(Number(timestamp) * 1000);
        return obj;
    });
}

/*
Takes the run data structured like the above and returns an aggregation like this:
{
    agents: [a1, a2, a3],
    results: {
        startupDurationMs: {
          a1: 123,
          a2: 334,
          a3: 234
        },
        totalAllocatedMB: {
          a1: 33838,
          a2: 230984,
          a3: 3893
        }
        ...
    }
}
 */
function aggregateRunData(data) {
    const firstRun = data['runs'][0];
    const firstAgentRun = Object.entries(firstRun)[0];
    const fields = Object.keys(firstAgentRun[1]);
    console.log(fields)
    const res = fields.map(field => {
        const results = aggregateSingleResult(data, field);
        return [field, results];
    });
    return {
        agents: data['agents'],
        results: Object.fromEntries(res)
    };
}

function aggregateSingleResult(data, field) {
    const agentWithAverage = data.agents.map(agent => {
        const value = data.runs.reduce((acc,run) => acc + run[agent][field], 0);
        return [agent, value/data.runs.length];
    });
    return Object.fromEntries(agentWithAverage);
}
