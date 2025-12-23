export function setIframeContent(iframe, htmlContent, baseUrl) {
    if (!iframe) return;
    
    const doc = iframe.contentDocument || iframe.contentWindow.document;
    
    let processedHtml = htmlContent;
    
    if (baseUrl && baseUrl.length > 0) {
        try {
            const url = new URL(baseUrl);
            const baseHref = url.origin;
            
            if (!processedHtml.includes('<base')) {
                processedHtml = processedHtml.replace(
                    /<head([^>]*)>/i,
                    `<head$1><base href="${baseHref}/">`
                );
                
                if (!processedHtml.includes('<head')) {
                    processedHtml = `<base href="${baseHref}/">` + processedHtml;
                }
            }
        } catch (e) {
            console.warn('Invalid base URL for HTML viewer:', baseUrl);
        }
    }
    
    doc.open();
    doc.write(processedHtml);
    doc.close();
}

