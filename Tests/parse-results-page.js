const rows = document.querySelectorAll('.res table tr');

const statements = Array.from(rows.values()).map(tr => {
    const phone = tr.children[0].textContent;

    const fullNameParts = tr.children[1].textContent.split(' ');

    const surname = fullNameParts[0];
    const initials = fullNameParts.slice(1).join(' ');

    const address = tr.children[2].textContent;

    return `records.Add(new(
    "${surname}",
    "${initials}",
    "${phone}",
    "${address}",
    city
));`
});

const text = statements.reduce((joined, s) => joined + '\n\n' + s);

const pre = document.createElement('pre');
pre.textContent = text;

document.body.appendChild(pre);
