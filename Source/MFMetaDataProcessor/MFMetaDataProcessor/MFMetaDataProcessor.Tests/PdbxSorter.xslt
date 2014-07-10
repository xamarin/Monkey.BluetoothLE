<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
  
  <xsl:output method="xml" indent="yes"/>

  <xsl:template match="Classes">
    <xsl:copy>
      <xsl:apply-templates select="Class">
        <xsl:sort select="Token/TinyCLR"/>
      </xsl:apply-templates>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="Methods">
    <xsl:copy>
      <xsl:apply-templates select="Method">
        <xsl:sort select="Token/TinyCLR"/>
      </xsl:apply-templates>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="Fields">
    <xsl:copy>
      <xsl:apply-templates select="Field">
        <xsl:sort select="Token/TinyCLR"/>
      </xsl:apply-templates>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="@* | node()">
    <xsl:copy>
      <xsl:apply-templates select="@* | node()"/>
    </xsl:copy>
  </xsl:template>

</xsl:stylesheet>
