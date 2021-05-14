(() => {
    // Find all heading that should be formatted.
    const dateTimeHeadings = document.getElementsByClassName("utc-date-time") as HTMLCollectionOf<HTMLHeadingElement>;
    // Configure the formatting to follow the options below.
    const options: Intl.DateTimeFormatOptions = {
        year: "numeric",
        month: "long",
        day: "numeric",
        weekday: "long",
        hour: "numeric",
        minute: "numeric"
    };

    // Perform formatting for each of the elements.
    for (let i = 0; i < dateTimeHeadings.length; i++) {
        const heading = dateTimeHeadings[i];
        const dateTimeString = heading.innerText;
        heading.innerText = new Date(dateTimeString).toLocaleString(undefined, options);
    }
})();
