﻿export async function getEmailAdress() {
    try {
        // Make a GET request to the /.auth/me endpoint
        const response = await fetch("https://traspaso-cartera.azurewebsites.net/.auth/me", {
            method: "GET",
            headers: {
                "Content-Type": "application/json",
            },
        });
        if (response.ok) {
            // Parse the JSON response
            const userData = await response.json();
            const userClaims = userData[0].user_claims;
            const emailClaim = userClaims.find(claim => claim.typ === "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
            if (emailClaim) {
                return emailClaim.val;
            } else {
                console.error("Email address claim not found in response.");
                return null;
            }
        } else {
            console.error(`Failed to fetch email address: ${response.status} ${response.statusText}`);
            return null;
        }
    } catch (error) {
        console.error("Error fetching email address:", error);
        return null;
    }
}
