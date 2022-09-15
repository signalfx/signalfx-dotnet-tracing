import http from 'k6/http';
import {check} from "k6";

const baseUri = `http://${__ENV.ESHOP_HOSTNAME}:80/api`;

function randItem(list) {
    return list[rand(list.length)];
}

function rand(max) {
    return Math.floor(Math.random() * Math.floor(max))
}

function randomItem(items) {
    const toDuplicate = randItem(items);
    return {
        catalogBrandId: 2,
        catalogTypeId: 2,
        description: `short`,
        // name is being checked for uniqueness during insertion
        name: Math.random().toString(16),
        pictureUri: toDuplicate.pictureUri,
        price: toDuplicate.price + 1
    };
}

export default function () {

    const catalogItems = `${baseUri}/catalog-items`;
    const catalogRetrieval = http.get(catalogItems);
    check(catalogRetrieval, {"retrieve catalog status 200": r => r.status === 200});

    const catalog = JSON.parse(catalogRetrieval.body);

    // Add new items to the list
    const newItem1 = randomItem(catalog.catalogItems);
    const newItem2 = randomItem(catalog.catalogItems);
    const newItem3 = randomItem(catalog.catalogItems);

    const itemUrl = `${baseUri}/catalog-items`;

    const itemAdditions = http.batch([
        ["POST", itemUrl, JSON.stringify(newItem1), {headers: {'Content-Type': 'application/json'}}],
        ["POST", itemUrl, JSON.stringify(newItem2), {headers: {'Content-Type': 'application/json'}}],
        ["POST", itemUrl, JSON.stringify(newItem3), {headers: {'Content-Type': 'application/json'}}],
    ]);

    check(itemAdditions[0], {"add item status 200": r => r.status === 200});
    check(itemAdditions[1], {"add item status 200": r => r.status === 200});
    check(itemAdditions[2], {"add item status 200": r => r.status === 200});

    // Verify they can be retrieved
    const itemRetrievals = http.batch([
        ["GET", `${itemUrl}/${JSON.parse(itemAdditions[0].body).catalogItem.id}`],
        ["GET", `${itemUrl}/${JSON.parse(itemAdditions[1].body).catalogItem.id}`],
        ["GET", `${itemUrl}/${JSON.parse(itemAdditions[2].body).catalogItem.id}`]
    ]);

    check(itemRetrievals[0], {"retrieve item status 200": r => r.status === 200});
    check(itemRetrievals[1], {"retrieve item status 200": r => r.status === 200});
    check(itemRetrievals[2], {"retrieve item status 200": r => r.status === 200});

    // Clean up
    const itemDeletions = http.batch([
        ["DELETE", `${itemUrl}/${JSON.parse(itemAdditions[0].body).catalogItem.id}`],
        ["DELETE", `${itemUrl}/${JSON.parse(itemAdditions[1].body).catalogItem.id}`],
        ["DELETE", `${itemUrl}/${JSON.parse(itemAdditions[2].body).catalogItem.id}`]
    ]);

    check(itemDeletions[0], {"delete item status 200": r => r.status === 200});
    check(itemDeletions[1], {"delete item status 200": r => r.status === 200});
    check(itemDeletions[2], {"delete item status 200": r => r.status === 200});
}