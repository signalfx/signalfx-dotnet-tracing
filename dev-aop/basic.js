import http from 'k6/http';
import {check} from "k6";

const baseUri = `http://${__ENV.app_name}:80/api`;
// console.log(`running test against: ${baseUri}`);

function randItem(list){
    return list[rand(list.length)];
}

function rand(max){
    return Math.floor(Math.random() * Math.floor(max))
}

function randomItem(items) {
    const toDuplicate = randItem(items);
    return {
        // for now, use fixed type/brand, duplicate other values from a random item from the list
        catalogBrandId: 1,
        catalogTypeId: 1,
        description: `duplicate for ${toDuplicate.id}`,
        name: `duplicated from ${toDuplicate.name}`,
        pictureUri: toDuplicate.pictureUri,
        price: toDuplicate.price + 1
    };
}

export default function () {
    
    const catalogItems = `${baseUri}/catalog/list`;
    const catalogRetrieval = http.get(catalogItems);
    check(catalogRetrieval, { "retrieve catalog status 200": r => r.status === 200 });
    
    const catalog = JSON.parse(catalogRetrieval.body);
        
    // Add new items to the list
    const newItem1 = randomItem(catalog.catalogItems);
    const newItem2 = randomItem(catalog.catalogItems);
    const newItem3 = randomItem(catalog.catalogItems);

    const itemUrl = `${baseUri}/catalog-items`;
    
    const itemAdditions = http.batch([
        ["POST", itemUrl, JSON.stringify(newItem1), { headers: { 'Content-Type': 'application/json' } } ],
        ["POST", itemUrl, JSON.stringify(newItem2), { headers: { 'Content-Type': 'application/json' } } ],
        ["POST", itemUrl, JSON.stringify(newItem3), { headers: { 'Content-Type': 'application/json' } } ],
    ]);

    check(itemAdditions[0], { "add item status 200": r => r.status === 200});
    check(itemAdditions[1], { "add item status 200": r => r.status === 200});
    check(itemAdditions[2], { "add item status 200": r => r.status === 200});

    // Verify they can be retrieved
    const itemRetrievals = http.batch([
        ["GET", `${itemUrl}/${JSON.parse(itemAdditions[0].body).catalogItem.id}`],
        ["GET", `${itemUrl}/${JSON.parse(itemAdditions[1].body).catalogItem.id}`],
        ["GET", `${itemUrl}/${JSON.parse(itemAdditions[2].body).catalogItem.id}`]
    ]);
    
    check(itemRetrievals[0], { "retrieve item status 200": r => r.status === 200});
    check(itemRetrievals[1], { "retrieve item status 200": r => r.status === 200});
    check(itemRetrievals[2], { "retrieve item status 200": r => r.status === 200});

    // Clean up
    const itemDeletions = http.batch([
        ["DELETE", `${itemUrl}/${JSON.parse(itemAdditions[0].body).catalogItem.id}`],
        ["DELETE", `${itemUrl}/${JSON.parse(itemAdditions[1].body).catalogItem.id}`],
        ["DELETE", `${itemUrl}/${JSON.parse(itemAdditions[2].body).catalogItem.id}`]
    ]);

    check(itemDeletions[0], { "delete item status 200": r => r.status === 200});
    check(itemDeletions[1], { "delete item status 200": r => r.status === 200});
    check(itemDeletions[2], { "delete item status 200": r => r.status === 200});
}