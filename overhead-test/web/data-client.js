
const RESULTS_DIR = "results";

// Returns a list of available historic runs
async function getRuns(){
    return fetch(`${RESULTS_DIR}/index.txt`)
        .then(resp => resp.text())
        .then(body => body.split("\n").filter(x => x !== 'index.txt'));
}

async function getResults(name, config){
    return fetch(`${RESULTS_DIR}/${name}/results.csv`)
        .then(resp => resp.text())
        .then(body => parseCsv(body, config))
        .then(data => {
            const aggregated = aggregateRunData(data);
            console.log(aggregated)
            return aggregated;
        });
}

async function getConfig(name){
    const path = `${RESULTS_DIR}/${name}/config.json`;
    return fetch(path)
        .then(resp => resp.json())
        .catch(e => {
            console.log(`No such config found for ${path}`)
            return {};
    });
}